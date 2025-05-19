// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor
{
    internal interface IBuildSchemeContextualActions
    {
        public bool IsPrimary { get; }

        public void CopyCreateRequested();
        public void InheritCreateRequested();
        public void RemoveRequested();
        public void PreBuildRequested();
        public void BuildRequested();
        public void SetAsPrimaryRequested();
        public void UnsetPrimaryRequested();

        public static void PopulateMenu(Func<IBuildSchemeContextualActions> getOptions, string actionNamePrefix,
            DropdownMenu menu)
        {
            menu.AppendAction($"{actionNamePrefix}Apply Pre-build Now", _ => getOptions().PreBuildRequested());
            menu.AppendAction($"{actionNamePrefix}Build Player Now...", _ => getOptions().BuildRequested());
            menu.AppendSeparator(actionNamePrefix);
            menu.AppendAction($"{actionNamePrefix}Copy...", _ => getOptions().CopyCreateRequested());
            menu.AppendAction($"{actionNamePrefix}Inherit...", _ => getOptions().InheritCreateRequested());
            menu.AppendAction($"{actionNamePrefix}Remove", _ => getOptions().RemoveRequested());
            menu.AppendAction($"{actionNamePrefix}Unset Primary", _ => getOptions().UnsetPrimaryRequested(),
                _ => getOptions().IsPrimary ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden);
            menu.AppendAction($"{actionNamePrefix}Set as Primary", _ => getOptions().SetAsPrimaryRequested(),
                _ => getOptions().IsPrimary ? DropdownMenuAction.Status.Hidden : DropdownMenuAction.Status.Normal);
        }
    }
}
