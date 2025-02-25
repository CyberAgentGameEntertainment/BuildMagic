// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuildMagicEditor
{
    /// <summary>
    ///     The build scheme.
    /// </summary>
    [Serializable]
    public class BuildScheme : IBuildScheme, ISerializationCallbackReceiver
    {
        [SerializeField] private string _name;
        [SerializeReference] private List<IBuildConfiguration> _postBuildConfigurations = new();
        [SerializeReference] private List<IBuildConfiguration> _internalPrepareConfigurations = new();
        [SerializeReference] private List<IBuildConfiguration> _preBuildConfigurations = new();

        [SerializeField] private string _baseSchemeName;

        /// <inheritdoc cref="IBuildScheme.Name" />
        public string Name
        {
            get => _name;
            internal set => _name = value;
        }

        /// <inheritdoc cref="IBuildScheme.PreBuildConfigurations" />
        public IReadOnlyList<IBuildConfiguration> PreBuildConfigurations => _preBuildConfigurations;

        /// <inheritdoc cref="IBuildScheme.InternalPrepareConfigurations" />
        public IReadOnlyList<IBuildConfiguration> InternalPrepareConfigurations => _internalPrepareConfigurations;

        /// <inheritdoc cref="IBuildScheme.PostBuildConfigurations" />
        public IReadOnlyList<IBuildConfiguration> PostBuildConfigurations => _postBuildConfigurations;

        public string BaseSchemeName
        {
            get => _baseSchemeName;
            internal set => _baseSchemeName = value;
        }

        internal void AddPreBuildConfiguration(IBuildConfiguration configuration)
        {
            _preBuildConfigurations.Add(configuration);
        }

        internal void AddInternalPrepareConfiguration(IBuildConfiguration configuration)
        {
            _internalPrepareConfigurations.Add(configuration);
        }
        
        internal void AddPostBuildConfiguration(IBuildConfiguration configuration)
        {
            _postBuildConfigurations.Add(configuration);
        }

        internal void RemovePreBuildConfiguration(int index)
        {
            _preBuildConfigurations.RemoveAt(index);
        }

        internal void RemovePrepareBuildPlayerConfiguration(int index)
        {
            _internalPrepareConfigurations.RemoveAt(index);
        }
        
        internal void RemovePostBuildConfiguration(int index)
        {
            _postBuildConfigurations.RemoveAt(index);
        }

        public void OnBeforeSerialize()
        {
            RemoveNulls();
        }

        public void OnAfterDeserialize()
        {
            RemoveNulls();
        }

        private void RemoveNulls()
        {
            _postBuildConfigurations.RemoveAll(configuration => configuration == null);
            _internalPrepareConfigurations.RemoveAll(configuration => configuration == null);
            _preBuildConfigurations.RemoveAll(configuration => configuration == null);
        }
    }
}
