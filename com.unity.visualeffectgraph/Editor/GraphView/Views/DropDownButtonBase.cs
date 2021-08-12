using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class DropDownButtonBase : VisualElement
    {
        readonly bool m_HasLeftSeparator;
        readonly Button m_MainButton;

        EditorWindow m_CurrentPopup;

        protected readonly VisualElement m_PopupContent;


        protected DropDownButtonBase(
            string uxmlSource,
            string mainButtonLabel,
            string mainButtonName,
            string iconPath,
            bool hasSeparatorBefore = false,
            bool hasSeparatorAfter = false)
        {
            style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            if (hasSeparatorBefore)
            {
                m_HasLeftSeparator = true;
                var divider = new VisualElement();
                divider.AddToClassList("separator");
                Add(divider);
            }

            m_MainButton = new Button(OnMainButton) { name = mainButtonName };
            m_MainButton.AddToClassList("dropdown-button");
            m_MainButton.AddToClassList("unity-toolbar-toggle");
            if (!string.IsNullOrEmpty(iconPath))
            {
                var icon = new Image {image = EditorGUIUtility.LoadIcon(iconPath)};
                m_MainButton.Add(icon);
                tooltip = mainButtonLabel;
            }
            else
            {
                m_MainButton.text = mainButtonLabel;
            }
            Add(m_MainButton);

            var separator = new VisualElement();
            separator.AddToClassList("dropdown-separator");
            Add(separator);

            var dropDownButton = new Button(OnOpenPopupInternal);
            dropDownButton.AddToClassList("dropdown-arrow");
            dropDownButton.AddToClassList("unity-toolbar-toggle");
            dropDownButton.Add(new VisualElement());
            Add(dropDownButton);

            if (hasSeparatorAfter)
            {
                var divider = new VisualElement();
                divider.AddToClassList("separator");
                Add(divider);
            }

            m_PopupContent = new VisualElement();
            var tpl = VFXView.LoadUXML(uxmlSource);
            tpl.CloneTree(m_PopupContent);
            contentContainer.AddStyleSheetPath("VFXToolbar");
        }

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
            m_CurrentPopup.rootVisualElement.AddStyleSheetPath("VFXToolbar");
            m_CurrentPopup.rootVisualElement.Add(m_PopupContent);
            m_CurrentPopup.rootVisualElement.AddToClassList("popup");

            OnOpenPopup();
            var bounds = new Rect(GetPopupPosition(), localBound.size);
            // Offset the bounds to align the popup with the real dropdown left edge
            if (m_HasLeftSeparator)
            {
                bounds.xMin += 6;
            }

            m_CurrentPopup.ShowAsDropDown(bounds, GetPopupSize(), new[] { PopupLocation.BelowAlignLeft, PopupLocation.AboveAlignLeft });
        }
    }
}
