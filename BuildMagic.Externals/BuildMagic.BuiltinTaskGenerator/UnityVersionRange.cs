// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace BuildMagic.BuiltinTaskGenerator;

/// <summary>
///     Represents a range of Unity versions (a single segment)
/// </summary>
/// <param name="Since"></param>
/// <param name="Until"></param>
public record UnityVersionRangeSegment(UnityVersion Since, UnityVersion Until) // both inclusive
{
    /// <summary>
    ///     Try to union the ranges if possible
    /// </summary>
    /// <param name="other"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool TryUnion(UnityVersionRangeSegment other, out UnityVersionRangeSegment result)
    {
        result = default;
        if (Until < other.Since || other.Until < Since) return false;

        result = new UnityVersionRangeSegment(
            Since < other.Since ? Since : other.Since,
            Until > other.Until ? Until : other.Until
        );
        return true;
    }

    public static implicit operator UnityVersionRange(UnityVersionRangeSegment src)
    {
        return new UnityVersionRange(src);
    }
}

/// <summary>
///     Represents a range of Unity versions (multiple segments)
/// </summary>
public class UnityVersionRange
{
    private List<UnityVersionRangeSegment> _segments = new();

    public UnityVersionRange()
    {
    }

    public UnityVersionRange(UnityVersionRangeSegment segment)
    {
        _segments.Add(segment);
    }

    public IEnumerable<UnityVersionRangeSegment> Segments => _segments;

    /// <summary>
    ///     Union the ranges if possible
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    /// <returns></returns>
    public static UnityVersionRange operator +(UnityVersionRange lhs, UnityVersionRange rhs)
    {
        var result = new UnityVersionRange();
        result._segments.AddRange(lhs._segments);

        foreach (var segOther in rhs._segments)
        {
            var cursor = segOther;

            for (var i = 0; i < result._segments.Count; i++)
            {
                var seg = result._segments[i];
                if (seg.TryUnion(cursor, out var newSeg))
                {
                    result._segments.RemoveAt(i);
                    i--;

                    cursor = newSeg;
                }
            }

            result._segments.Add(cursor);
        }

        result._segments = result._segments.OrderBy(s => s.Since).ToList();

        return result;
    }
}
