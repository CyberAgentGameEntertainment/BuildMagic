// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal sealed class TabView : VisualElement, IDisposable
    {
        private const string SelectedClassName = "selected";
        private const string TabClassName = "tab";

        /// <summary>
        ///     Callback for when the tab selected;
        /// </summary>
        public event Action<int> OnTabSelected;

        private UQueryBuilder<VisualElement> GetAllTabs() => this.Query(className: TabClassName);

        private void OnClick(ClickEvent evt)
            => OnClick(evt.currentTarget as VisualElement);

        private void OnClick(VisualElement clickedTab)
        {
            if (TabIsCurrentlySelected(clickedTab))
                return;

            GetAllTabs().Where(tab => tab != clickedTab && TabIsCurrentlySelected(tab))
                        .ForEach(tab => tab.RemoveFromClassList(SelectedClassName));
            clickedTab.AddToClassList(SelectedClassName);
            OnTabSelected?.Invoke(clickedTab.tabIndex);
        }

        private static bool TabIsCurrentlySelected(VisualElement tab) => tab.ClassListContains(SelectedClassName);

        public void Setup()
        {
            SetupEventHandlers();
        }

        public void SelectTab(int tabIndex)
        {
            GetAllTabs().Where(tab => tab.tabIndex == tabIndex).ForEach(OnClick);
        }

        private void SetupEventHandlers()
        {
            GetAllTabs().ForEach(tab => { tab.RegisterCallback<ClickEvent>(OnClick); });
        }

        private void CleanupEventHandlers()
        {
            GetAllTabs().ForEach(tab => { tab.UnregisterCallback<ClickEvent>(OnClick); });
        }
        
        public new class UxmlFactory : UnityEngine.UIElements.UxmlFactory<TabView, BindableElement.UxmlTraits>
        {
        }

        public void Dispose()
        {
            CleanupEventHandlers();
        }
    }
}
