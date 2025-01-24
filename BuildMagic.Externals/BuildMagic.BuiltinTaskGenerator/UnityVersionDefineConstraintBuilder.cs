// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildMagic.BuiltinTaskGenerator;

public class UnityVersionDefineConstraintBuilder
{
    private readonly UnityVersion[] _knownVersionsAscending;

    public UnityVersionDefineConstraintBuilder(IEnumerable<UnityVersion> knownVersions)
    {
        _knownVersionsAscending = knownVersions.Order().ToArray();

        if (_knownVersionsAscending.Length == 0)
            throw new ArgumentException("At least one version must be provided.", nameof(knownVersions));
    }

    public UnityVersion Earliest => _knownVersionsAscending[0];
    public UnityVersion Latest => _knownVersionsAscending[^1];

    public UnityVersion GetLatestRevision(UnityVersion version)
    {
        if (version.Major == Latest.Major && version.Minor == Latest.Minor) return Latest;

        for (var i = Array.IndexOf(_knownVersionsAscending, version) + 1; i < _knownVersionsAscending.Length; i++)
        {
            var v = _knownVersionsAscending[i];
            if (v.Major != version.Major || v.Minor != version.Minor) return _knownVersionsAscending[i - 1];
        }

        throw new InvalidOperationException();
    }

    private UnityVersionRange ToRange(IEnumerable<UnityVersion> versions)
    {
        UnityVersion? currentSince = null;
        UnityVersion? currentUntil = null;

        using var versionsIter = versions.GetEnumerator();

        if (!versionsIter.MoveNext()) return new UnityVersionRange();

        var result = new UnityVersionRange();

        foreach (var knownVersion in _knownVersionsAscending)
            if (knownVersion == versionsIter.Current)
            {
                currentSince ??= knownVersion;
                currentUntil = knownVersion;
                if (!versionsIter.MoveNext()) break;
            }
            else if (currentSince != null)
            {
                result += new UnityVersionRangeSegment(currentSince.Value, currentUntil.Value);
                currentSince = null;
                currentUntil = null;
            }

        if (currentSince != null) result += new UnityVersionRangeSegment(currentSince.Value, currentUntil.Value);

        return result;
    }

    public DefineConstraint? Get(IEnumerable<UnityVersion> versions, out bool isNever)
    {
        return Get(versions, out _, out isNever);
    }

    public DefineConstraint? Get(IEnumerable<UnityVersion> versions, out UnityVersionRange range, out bool isNever)
    {
        range = ToRange(versions);

        if (!range.Segments.Any())
        {
            // empty
            isNever = true;
            return null;
        }

        isNever = false;

        DefineConstraint? allConstraint = null;
        foreach (var segment in range.Segments)
        {
            var start = segment.Since;
            var end = segment.Until;

            // it is valid for the earliest supported version, so make it valid for versions before that
            var firstAvailable = start == Earliest;

            // it is valid for the latest supported version, so make it valid for versions after that
            var latestAvailable = end == Latest;

            var minorLocal = start.Major == end.Major && start.Minor == end.Minor;

            DefineConstraint? constraint = null;

            var startIndex = Array.IndexOf(_knownVersionsAscending, start);
            var endIndex = Array.IndexOf(_knownVersionsAscending, end);

            // Future revisions of the non-latest minor version are valid, reducing the diff of generated code and making it easier to support new Unity releases.
            // (e.g. 7000.0.x is already out, but 6000.1.x series is still getting updates, and items that are valid in the latest 6000.1.x but not in 7000.0.0 should be valid for future 6000.1.x versions)

            var latestRevisionInPrevMinor = !latestAvailable && _knownVersionsAscending[endIndex + 1].Revision == 0;

            if (minorLocal && !latestAvailable && !firstAvailable)
            {
                var needNewerFlag = latestRevisionInPrevMinor;

                var activeInMinor = _knownVersionsAscending.Count(c => start <= c && c <= end);
                var allMinor = _knownVersionsAscending.Count(c => c.Major == start.Major && c.Minor == start.Minor);
                var inactiveInMinor = allMinor - activeInMinor;

                var useNewerFlag = needNewerFlag || inactiveInMinor < activeInMinor; // prefer short form

                if (useNewerFlag)
                {
                    var c = $"UNITY_{start.Major}_{start.Minor}_OR_NEWER".ToDefine();
                    var nextMinorIndex = -1;
                    for (var i = endIndex + 1; i < _knownVersionsAscending.Length; i++)
                    {
                        var v = _knownVersionsAscending[i];
                        if (v.Major != end.Major || v.Minor != end.Minor)
                        {
                            nextMinorIndex = i;
                            break;
                        }

                        c &= !$"UNITY_{v.Major}_{v.Minor}_{v.Revision}".ToDefine();
                    }

                    if (nextMinorIndex != -1)
                    {
                        var nextMinor = _knownVersionsAscending[nextMinorIndex];
                        c &= !$"UNITY_{nextMinor.Major}_{nextMinor.Minor}_OR_NEWER".ToDefine();
                    }

                    // negate inactive versions before start in the same minor
                    for (var i = startIndex - 1; i > 0; i--)
                    {
                        var v = _knownVersionsAscending[i];
                        if (v.Major != start.Major || v.Minor != start.Minor)
                            break;
                        c &= !$"UNITY_{v.Major}_{v.Minor}_{v.Revision}".ToDefine();
                    }

                    constraint = c;
                }
                else
                {
                    for (var i = startIndex; i <= endIndex; i++)
                    {
                        var ver = _knownVersionsAscending[i];

                        var c = $"UNITY_{ver.Major}_{ver.Minor}_{ver.Revision}".ToDefine();

                        if (constraint == null)
                            constraint = c;
                        else
                            constraint |= c;
                    }
                }
            }
            else
            {
                if (!latestAvailable)
                {
                    var nextVerIndex = Array.IndexOf(_knownVersionsAscending, end) + 1;
                    var nextVer = _knownVersionsAscending[nextVerIndex];

                    if (latestRevisionInPrevMinor)
                    {
                        constraint = !$"UNITY_{nextVer.Major}_{nextVer.Minor}_OR_NEWER".ToDefine();
                    }
                    else
                    {
                        // It is terminated in the middle of the minor version.
                        // To making sure that the constraint is valid for future minor versions, we need to "include" all minor version until the end, not "exclude" all minor versions after the end.

                        constraint = !$"UNITY_{end.Major}_{end.Minor}_OR_NEWER".ToDefine();

                        DefineConstraint? individualVersions = null;

                        for (var i = endIndex; i >= 0; i--)
                        {
                            var ver = _knownVersionsAscending[i];
                            if (ver.Major != end.Major || ver.Minor != end.Minor) break;
                            if (ver < start) break;

                            var c = $"UNITY_{ver.Major}_{ver.Minor}_{ver.Revision}".ToDefine();

                            if (individualVersions == null)
                                individualVersions = c;
                            else
                                individualVersions |= c;
                        }

                        if (individualVersions != null) constraint |= individualVersions;
                    }
                }

                if (!firstAvailable)
                {
                    // "include" after the start

                    var low = $"UNITY_{start.Major}_{start.Minor}_OR_NEWER".ToDefine();
                    if (constraint != null)
                        constraint &= low;
                    else
                        constraint = low;

                    if (start.Revision != 0)
                    {
                        // XXXX.Y.ZZ

                        DefineConstraint? individualVersions = null;

                        for (var i = startIndex - 1; i >= 0; i--)
                        {
                            var ver = _knownVersionsAscending[i];
                            if (ver.Major != start.Major || ver.Minor != start.Minor) break;
                            var c = !$"UNITY_{ver.Major}_{ver.Minor}_{ver.Revision}".ToDefine();
                            if (individualVersions == null)
                                individualVersions = c;
                            else
                                individualVersions &= c;
                        }

                        if (individualVersions != null) constraint &= individualVersions;
                    }
                }
            }

            if (allConstraint == null) allConstraint = constraint;
            else allConstraint |= constraint;
        }

        return allConstraint;
    }
}
