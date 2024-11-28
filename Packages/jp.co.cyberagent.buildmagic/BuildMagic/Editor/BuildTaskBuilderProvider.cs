// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildMagicEditor
{
    /// <summary>
    ///     The provider of <see cref="IBuildTaskBuilder{TTask,TValue}" />.
    /// </summary>
    public partial class BuildTaskBuilderProvider
    {
        private readonly Dictionary<(Type, Type), IBuildTaskBuilder> _builders = new();

        private BuildTaskBuilderProvider()
        {
        }

        // implemented in generated source
        private partial void RegisterBuiltInBuilder();

        public static BuildTaskBuilderProvider CreateDefault()
        {
            var builder = new BuildTaskBuilderProvider();

            builder.RegisterBuiltInBuilder();

            foreach (var type in TypeCache.GetTypesWithAttribute<BuildTaskBuilderAttribute>())
            {
                if (!typeof(IBuildTaskBuilder).IsAssignableFrom(type))
                    continue;

                IBuildTaskBuilder builderInstance = null;
                foreach (var attribute in type.CustomAttributes.Where(attr =>
                             attr.AttributeType == typeof(BuildTaskBuilderAttribute)))
                {
                    var taskType = attribute.ConstructorArguments[0].Value as Type;
                    var valueType = attribute.ConstructorArguments[1].Value as Type;

                    if (taskType == null || !typeof(IBuildTask).IsAssignableFrom(taskType))
                        Debug.LogWarning(
                            $"IBuildTaskBuilder \"{type}\" is marked with task type \"{taskType}\" which is not a valid task type");

                    builder._builders[(taskType, valueType)] =
                        builderInstance ??= Activator.CreateInstance(type) as IBuildTaskBuilder;
                }
            }

            return builder;
        }

        /// <summary>
        ///     Register the builder for the specified task and value.
        /// </summary>
        /// <typeparam name="TTask">The type of the task.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="builder">The builder instance.</param>
        public void Register<TTask, TValue>(IBuildTaskBuilder<TTask, TValue> builder) where TTask : IBuildTask
        {
            _builders[(typeof(TTask), typeof(TValue))] = builder;
        }

        /// <summary>
        ///     Get the builder for the specified task and value.
        /// </summary>
        /// <param name="taskType">The type of the task.</param>
        /// <param name="valueType">The type of the value.</param>
        /// <returns>A builder corresponding to the specified types.</returns>
        /// <exception cref="InvalidOperationException">Throws if no corresponding to the specified types found.</exception>
        public IBuildTaskBuilder GetBuilder(Type taskType, Type valueType)
        {
            if (_builders.TryGetValue((taskType, valueType), out var builder))
                return builder;

            throw new InvalidOperationException(
                $"Builder for {taskType.Name} and {valueType.Name} is not registered.");
        }
    }
}
