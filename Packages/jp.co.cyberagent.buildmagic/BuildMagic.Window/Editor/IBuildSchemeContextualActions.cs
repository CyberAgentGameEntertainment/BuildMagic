// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor
{
    internal interface IBuildSchemeContextualActions
    {
        public bool IsActive { get; }
        public bool IsPrimary { get; }

        public void CopyCreateRequested();
        public void InheritCreateRequested();
        public void RemoveRequested();
        public void PreBuildRequested();
        public void BuildRequested();
        public void SetAsPrimaryRequested();
        public void UnsetPrimaryRequested();

        public static void PopulateMenu(Func<IBuildSchemeContextualActions> getOptions, DropdownMenu menu,
            string subMenuPath = null)
        {
            Func<DropdownMenuAction, DropdownMenuAction.Status> getStatus = _ =>
                getOptions().IsActive ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
            var prefix = string.IsNullOrEmpty(subMenuPath) ? string.Empty : $"{subMenuPath}/";
            menu.AppendAction($"{prefix}Apply Pre-build Now", _ => getOptions().PreBuildRequested(),
                getStatus);
            menu.AppendAction($"{prefix}Build Player Now...", _ => getOptions().BuildRequested(), getStatus);
            menu.AppendSeparator(prefix);
            menu.AppendAction($"{prefix}Copy...", _ => getOptions().CopyCreateRequested(), getStatus);
            menu.AppendAction($"{prefix}Inherit...", _ => getOptions().InheritCreateRequested(), getStatus);
            menu.AppendAction($"{prefix}Remove", _ => getOptions().RemoveRequested(), getStatus);
            menu.AppendAction($"{prefix}Unset Primary", _ => getOptions().UnsetPrimaryRequested(),
                _ => getOptions() switch
                {
                    { IsPrimary: false } => DropdownMenuAction.Status.Hidden,
                    { IsActive: false } => DropdownMenuAction.Status.Disabled,
                    _ => DropdownMenuAction.Status.Normal
                });
            menu.AppendAction($"{prefix}Set as Primary", _ => getOptions().SetAsPrimaryRequested(),
                _ => getOptions() switch
                {
                    { IsPrimary: true } => DropdownMenuAction.Status.Hidden,
                    { IsActive: false } => DropdownMenuAction.Status.Disabled,
                    _ => DropdownMenuAction.Status.Normal
                });
        }
    }
}
