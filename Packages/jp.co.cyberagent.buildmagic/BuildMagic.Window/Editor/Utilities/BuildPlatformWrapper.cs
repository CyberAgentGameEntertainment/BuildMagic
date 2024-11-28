// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Build;
using UnityEngine;

namespace BuildMagic.Window.Editor.Elements
{
    internal readonly struct BuildPlatformWrapper : IEquatable<BuildPlatformWrapper>
    {
        public bool IsValid => _buildPlatform != null;
        private readonly object _buildPlatform;
        public NamedBuildTarget NamedBuildTarget { get; }
        public Texture2D SmallIcon { get; }

        static BuildPlatformWrapper()
        {
            var buildPlatformsType =
                typeof(NamedBuildTarget).Assembly.GetType("UnityEditor.Build.BuildPlatforms");
            var buildPlatformsInstance = buildPlatformsType.GetProperty("instance")
                .GetValue(null);

            var platforms = (IReadOnlyList<object>)buildPlatformsType
                .GetMethod("GetValidPlatforms", 0, new[] { typeof(bool) })
                .Invoke(buildPlatformsInstance, new object[] { true });

            ValidBuildPlatforms = platforms.Select(p => new BuildPlatformWrapper(p)).ToArray();

            BuildPlatforms =
                (buildPlatformsType.GetField("buildPlatforms", BindingFlags.Instance | BindingFlags.Public)
                    .GetValue(buildPlatformsInstance) as object[]).Select(p => new BuildPlatformWrapper(p))
                .ToArray();
        }

        public static BuildPlatformWrapper[] ValidBuildPlatforms { get; }
        public static BuildPlatformWrapper[] BuildPlatforms { get; }

        private BuildPlatformWrapper(object buildPlatform)
        {
            _buildPlatform = buildPlatform;
            var buildPlatformType =
                typeof(NamedBuildTarget).Assembly.GetType("UnityEditor.Build.BuildPlatform");

            NamedBuildTarget = (NamedBuildTarget)buildPlatformType.GetField("namedBuildTarget").GetValue(buildPlatform);
            SmallIcon = (Texture2D)buildPlatformType.GetProperty("smallIcon").GetValue(buildPlatform);
        }

        public bool Equals(BuildPlatformWrapper other)
        {
            return Equals(_buildPlatform, other._buildPlatform);
        }

        public override bool Equals(object obj)
        {
            return obj is BuildPlatformWrapper other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _buildPlatform != null ? _buildPlatform.GetHashCode() : 0;
        }
    }
}
