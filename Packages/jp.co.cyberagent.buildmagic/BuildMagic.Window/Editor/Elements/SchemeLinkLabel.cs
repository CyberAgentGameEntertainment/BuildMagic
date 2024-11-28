// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    public class SchemeLinkLabel : BindableElement, INotifyValueChanged<string>
    {
        private readonly Label _leadingLabel;
        private readonly Label _linkLabel;

        private string _value;

        public SchemeLinkLabel()
        {
            _leadingLabel = new Label("derived from");
            _linkLabel = new Label();
            Add(_leadingLabel);
            Add(_linkLabel);
            _leadingLabel.AddToClassList("scheme-link-label-leading");
            _linkLabel.AddToClassList("scheme-link-label-link");
            _linkLabel.RegisterCallback<PointerDownEvent, SchemeLinkLabel>(
                static (evt, @this) =>
                {
                    EditorWindow.GetWindow<BuildMagicWindow>().SelectScheme(@this._value);
                }, this);
        }

        #region Nested type: UxmlFactory

        public new class UxmlFactory : UxmlFactory<SchemeLinkLabel, UxmlTraits>
        {
        }

        #endregion

        #region Nested type: UxmlTraits

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription _text;

            public UxmlTraits()
            {
                var attributeDescription = new UxmlStringAttributeDescription();
                attributeDescription.name = "text";
                _text = attributeDescription;
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var element = (INotifyValueChanged<string>)ve;
                element.value = _text.GetValueFromBag(bag, cc);
            }
        }

        #endregion

        #region INotifyValueChanged<string> Members

        public void SetValueWithoutNotify(string newValue)
        {
            _value = newValue;
        }

        string INotifyValueChanged<string>.value
        {
            get => _value;
            set
            {
                _value = value;
                _leadingLabel.text = string.IsNullOrEmpty(value) ? "" : "derived from";
                _linkLabel.text = $"<u>{value}</u>";
            }
        }

        #endregion
    }
}
