// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Linq;
using UnityEngine;

namespace BuildMagicEditor
{
    /// <summary>
    ///     The base class for build configurations.
    /// </summary>
    [Serializable]
    public abstract class BuildConfigurationBase<TTask, TValue> : IBuildConfiguration
        where TTask : IBuildTask
    {
        /// <summary>
        ///     Actual value of the property.
        /// </summary>
        [SerializeField] private TValue _value;

        /// <summary>
        ///     The name of the property.
        /// </summary>
        public string PropertyName => BuildConfigurationTypeUtility.GetPropertyName(GetType());

        /// <inheritdoc cref="IBuildConfiguration.TaskType" />
        public Type TaskType => typeof(TTask);

        /// <inheritdoc cref="IBuildConfiguration.GatherProperty" />
        public IBuildProperty GatherProperty()
        {
            return BuildProperty<TValue>.Create(PropertyName, _value);
        }

        /// <summary>
        ///     The value of the property.
        /// </summary>
        protected TValue Value
        {
            get => _value;
            set => _value = value;
        }
    }
}
