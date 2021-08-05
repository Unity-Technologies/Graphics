using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    public interface IResponsiveElement
    {
        int Priority { get; }
        bool IsCompact { get; set;}
        bool CanCompact { get; }
        float GetSavedSpace();
    }

    abstract class DropDownButtonBase : VisualElement, IResponsiveElement
    {
        readonly bool m_HasLeftSeparator;
        readonly Button m_MainButton;
        readonly Label m_Label;

        EditorWindow m_CurrentPopup;

        protected readonly VisualElement m_PopupContent;
        private bool m_IsCompact;


        protected DropDownButtonBase(
            string uxmlSource,
            string mainButtonLabel,
            int priority,
            string icon = null,
            bool hasSeparatorBefore = false,
            bool hasSeparatorAfter = false)
        {
            style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            Priority = priority;

            if (hasSeparatorBefore)
            {
                m_HasLeftSeparator = true;
                var separator = new VisualElement();
                separator.AddToClassList("separator");
                Add(separator);
            }

            m_MainButton = new Button(OnMainButton) { name = "button" };
            m_MainButton.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            if (icon != null)
            {
                m_MainButton.Add(new Image { image = EditorGUIUtility.LoadIcon(icon) });
                m_Label = new Label(mainButtonLabel);
                m_MainButton.Add(m_Label);
            }
            else
            {
                m_MainButton.text = mainButtonLabel;
            }
            Add(m_MainButton);

            var dropDownButton = new Button(OnOpenPopupInternal) {name = "arrow" };
            dropDownButton.Add(new VisualElement());
            Add(dropDownButton);

            if (hasSeparatorAfter)
            {
                var separator = new VisualElement();
                separator.AddToClassList("separator");
                Add(separator);
            }

            m_PopupContent = new VisualElement();
            var tpl = VFXView.LoadUXML(uxmlSource);
            tpl.CloneTree(m_PopupContent);
            contentContainer.AddStyleSheetPath("VFXSaveDropDownPanel");
        }

        public int Priority { get; }

        public bool CanCompact => m_Label != null;

        public bool IsCompact
        {
            get => m_IsCompact;
            set => SetCompact(value);
        }

        public float GetSavedSpace() => m_Label.localBound.size.x;

        protected virtual void OnOpenPopup() {}
        protected virtual void OnMainButton() {}
        protected abstract Vector2 GetPopupPosition();
        protected abstract Vector2 GetPopupSize();

        protected void ClosePopup()
        {
            m_CurrentPopup?.Close();
            m_CurrentPopup = null;
        }

        private void OnOpenPopupInternal()
        {
            m_CurrentPopup = ScriptableObject.CreateInstance<EditorWindow>();
            m_CurrentPopup.hideFlags = HideFlags.HideAndDontSave;
            if (m_PopupContent.parent != null)
            {
                m_PopupContent.parent.Remove(m_PopupContent);
            }
            m_CurrentPopup.rootVisualElement.Add(m_PopupContent);
            m_CurrentPopup.rootVisualElement.AddToClassList("popup");
            m_CurrentPopup.rootVisualElement.AddStyleSheetPath("VFXSaveDropDownPanel");

            OnOpenPopup();
            var bounds = new Rect(GetPopupPosition(), localBound.size);
            // Offset the bounds to align the popup with the real dropdown left edge
            if (m_HasLeftSeparator)
            {
                bounds.xMin += 6;
            }
            m_CurrentPopup.ShowAsDropDown(bounds, GetPopupSize(), new [] { PopupLocation.BelowAlignLeft, PopupLocation.AboveAlignLeft });
        }

        private void SetCompact(bool isCompact)
        {
            if (CanCompact)
            {
                if (isCompact)
                {
                    m_MainButton.Remove(m_Label);
                    tooltip = m_Label.text;
                }
                else
                {
                    m_MainButton.Add(m_Label);
                    tooltip = null;
                }
                m_IsCompact = isCompact;
            }
        }
    }
}
