// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories(
        @"EditorUserBuildSettings: Compression Type",
        PropertyName = @"EditorUserBuildSettings.SetCompressionType()")]
    public unsafe class EditorUserBuildSettingsSetCompressionTypeTask : BuildTaskBase<IPreBuildContext>
    {
        private static delegate*<BuildTargetGroup, CompressionType, void> _setCompressionTypeMethod;

        private readonly IReadOnlyDictionary<BuildTargetGroup, CompressionType> _compressionTypes;

        public EditorUserBuildSettingsSetCompressionTypeTask(
            IReadOnlyDictionary<BuildTargetGroup, CompressionType> compressionTypes)
        {
            _compressionTypes = compressionTypes;
        }

        private static delegate*<BuildTargetGroup, CompressionType, void> SetCompressionTypeMethod =>
            (IntPtr)_setCompressionTypeMethod != IntPtr.Zero
                ? _setCompressionTypeMethod
                : _setCompressionTypeMethod =
                    (delegate*<BuildTargetGroup, CompressionType, void>)typeof(EditorUserBuildSettings)
                        .GetMethod("SetCompressionType", BindingFlags.NonPublic | BindingFlags.Static)?.MethodHandle
                        .GetFunctionPointer();

        public override void Run(IPreBuildContext context)
        {
            var setCompressionTypeMethod = SetCompressionTypeMethod;
            if ((IntPtr)setCompressionTypeMethod == IntPtr.Zero)
                throw new InvalidOperationException(
                    "Failed to find EditorUserBuildSettings.SetCompressionType() method");

            foreach (var compressionType in _compressionTypes)
                SetCompressionTypeMethod(compressionType.Key, compressionType.Value);
        }
    }

    unsafe partial class EditorUserBuildSettingsSetCompressionTypeTaskConfiguration : IProjectSettingApplier
    {
        private static delegate*<BuildTargetGroup, CompressionType> _getCompressionTypeMethod;

        private static delegate*<BuildTargetGroup, CompressionType> GetCompressionTypeMethod =>
            (IntPtr)_getCompressionTypeMethod != IntPtr.Zero
                ? _getCompressionTypeMethod
                : _getCompressionTypeMethod =
                    (delegate*<BuildTargetGroup, CompressionType>)typeof(EditorUserBuildSettings)
                        .GetMethod("GetCompressionType", BindingFlags.NonPublic | BindingFlags.Static)?.MethodHandle
                        .GetFunctionPointer();

        #region IProjectSettingApplier Members

        void IProjectSettingApplier.ApplyProjectSetting()
        {
            var getCompressionTypeMethod = GetCompressionTypeMethod;
            if ((IntPtr)getCompressionTypeMethod == IntPtr.Zero)
                throw new InvalidOperationException(
                    "Failed to find EditorUserBuildSettings.GetCompressionType() method");

            SerializableDictionary<BuildTargetGroup, CompressionType> value = new();

            foreach (var (buildTargetGroup, _) in Value)
                value[buildTargetGroup] = GetCompressionTypeMethod(buildTargetGroup);

            Value = value;
        }

        #endregion
    }
}
