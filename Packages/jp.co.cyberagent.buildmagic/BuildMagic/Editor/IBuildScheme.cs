// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Interface for build scheme.
    /// </summary>
    public interface IBuildScheme
    {
        /// <summary>
        ///     Name of the build setting.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The build configurations for the pre-build phase.
        /// </summary>
        IReadOnlyList<IBuildConfiguration> PreBuildConfigurations { get; }

        /// <summary>
        ///     The build configurations for the internal parepare phase.
        /// </summary>
        IReadOnlyList<IBuildConfiguration> InternalPrepareConfigurations { get; }

        /// <summary>
        ///     The build configurations for the post-build phase.
        /// </summary>
        IReadOnlyList<IBuildConfiguration> PostBuildConfigurations { get; }
    }
}
