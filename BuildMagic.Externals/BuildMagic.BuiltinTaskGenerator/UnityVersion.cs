// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagic.BuiltinTaskGenerator;

public readonly struct UnityVersion : IEquatable<UnityVersion>, IComparable<UnityVersion>
{
    private static readonly char[] ReleaseChars =
    [
        'a', // alpha
        'b', // beta
        'f', // final
        'p' // patch
    ];

    public int Major { get; }
    public int Minor { get; }
    public int Revision { get; }
    private int ReleaseTypeIndex { get; }
    public char ReleaseType => ReleaseChars[ReleaseTypeIndex];
    public int IncrementalVersion { get; }

    public UnityVersion(int major, int minor, int revision, char releaseType, int incrementalVersion)
    {
        Major = major;
        Minor = minor;
        Revision = revision;
        ReleaseTypeIndex = Array.IndexOf(ReleaseChars, releaseType);
        if (ReleaseTypeIndex == -1) throw new ArgumentOutOfRangeException(nameof(releaseType));
        IncrementalVersion = incrementalVersion;
    }

    public static bool TryParse(string version, out UnityVersion result)
    {
        result = default;

        var parts = version.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var major) ||
            !int.TryParse(parts[1], out var minor))
            return false;

        var releaseSep = parts[2].IndexOfAny(ReleaseChars);
        if (releaseSep == -1 || parts[2].Length <= releaseSep + 1) return false;
        var release = parts[2][releaseSep];

        if (!int.TryParse(parts[2][..releaseSep], out var revision) ||
            !int.TryParse(parts[2][(releaseSep + 1)..], out var incremental))
            return false;

        result = new UnityVersion(major, minor, revision, release, incremental);
        return true;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Revision}{ReleaseChars[ReleaseTypeIndex]}{IncrementalVersion}";
    }

    public bool Equals(UnityVersion other)
    {
        return Major == other.Major && Minor == other.Minor && Revision == other.Revision &&
               ReleaseTypeIndex == other.ReleaseTypeIndex && IncrementalVersion == other.IncrementalVersion;
    }

    public override bool Equals(object? obj)
    {
        return obj is UnityVersion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Revision, ReleaseTypeIndex, IncrementalVersion);
    }

    public static bool operator ==(UnityVersion left, UnityVersion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(UnityVersion left, UnityVersion right)
    {
        return !left.Equals(right);
    }

    public static bool operator <(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(UnityVersion left, UnityVersion right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static implicit operator string(UnityVersion source)
    {
        return source.ToString();
    }

    public static implicit operator UnityVersion(string source)
    {
        if (!TryParse(source, out var result)) throw new ArgumentException();
        return result;
    }

    public int CompareTo(UnityVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;
        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;
        var revisionComparison = Revision.CompareTo(other.Revision);
        if (revisionComparison != 0) return revisionComparison;
        var releaseTypeComparison = ReleaseTypeIndex.CompareTo(other.ReleaseTypeIndex);
        if (releaseTypeComparison != 0) return releaseTypeComparison;
        return IncrementalVersion.CompareTo(other.IncrementalVersion);
    }
}
