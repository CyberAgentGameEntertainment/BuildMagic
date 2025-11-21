// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.SubWindows
{
    public class CreateSchemeModalWindow : EditorWindow
    {
        private Context _context;

        private void CreateGUI()
        {
            var visualTree = AssetLoader.LoadUxml("SubWindows/CreateSchemeWindow");
            visualTree.CloneTree(rootVisualElement);

            var nameField = rootVisualElement.Q<TextField>("name-field");
            Assert.IsNotNull(nameField);

            var copySelectDropdown = rootVisualElement.Q<DropdownField>("copy-select-dropdown");
            Assert.IsNotNull(copySelectDropdown);

            var baseSelectDropdown = rootVisualElement.Q<DropdownField>("base-select-dropdown");
            Assert.IsNotNull(baseSelectDropdown);

            var createButton = rootVisualElement.Q<Button>("create-button");
            Assert.IsNotNull(createButton);

            copySelectDropdown.choices = _context.existingSchemeNames.Prepend("-").ToList();
            copySelectDropdown.index = string.IsNullOrEmpty(_context.copyFromName)
                ? 0
                : copySelectDropdown.choices.IndexOf(_context.copyFromName);

            baseSelectDropdown.choices = _context.existingSchemeNames.Prepend("-").ToList();
            baseSelectDropdown.index = string.IsNullOrEmpty(_context.baseSchemeName)
                ? 0
                : baseSelectDropdown.choices.IndexOf(_context.baseSchemeName);

            createButton.clicked += () =>
            {
                var value1 = nameField.text;
                Assert.IsNotNull(value1);
                Assert.IsFalse(string.IsNullOrWhiteSpace(value1));
                Assert.IsFalse(_context.existingSchemeNames.Contains(value1));

                var value2 = copySelectDropdown.index == 0
                    ? null
                    : copySelectDropdown.choices[copySelectDropdown.index];

                var value3 = baseSelectDropdown.index == 0
                    ? null
                    : baseSelectDropdown.choices[baseSelectDropdown.index];

                _context.callback(value1, value2, value3);
                Close();
            };
            createButton.SetEnabled(false);

            nameField.RegisterValueChangedCallback(evt =>
            {
                var value = evt.newValue;
                createButton.SetEnabled(string.IsNullOrWhiteSpace(value) == false
                                        && _context.existingSchemeNames.Contains(value) == false);
            });
        }

        internal static void OpenModal(in Context context)
        {
            var modal = CreateInstance<CreateSchemeModalWindow>();
            modal.titleContent = new GUIContent("Build Scheme Creator - Build Magic");
            modal._context = context;
            modal.ShowModalUtility();
        }

        public readonly struct Context
        {
            public readonly string copyFromName;
            public readonly string baseSchemeName;
            public readonly ICollection<string> existingSchemeNames;
            public readonly Action<string, string, string> callback;

            public Context(string copyFromName,
                           string baseSchemeName,
                ICollection<string> existingSchemeNames,
                Action<string, string, string> callback)
            {
                this.copyFromName = copyFromName;
                this.baseSchemeName = baseSchemeName;
                this.existingSchemeNames = existingSchemeNames;
                this.callback = callback;
            }
        }
    }
}
