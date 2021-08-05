using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class DropDownButtonBase : VisualElement
    {
        private readonly bool m_HasLeftSeparator;

        private EditorWindow m_CurrentPopup;

        protected readonly VisualElement m_PopupContent;


        protected DropDownButtonBase(string uxmlSource, string mainButtonLabel, string icon = null, bool hasSeparatorBefore = false, bool hasSeparatorAfter = false)
        {
            style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            if (hasSeparatorBefore)
            {
                m_HasLeftSeparator = true;
                var separator = new VisualElement();
                separator.AddToClassList("separator");
                Add(separator);
            }

            var toggleButton = new Button(OnMainButton) { name = "button" };
            toggleButton.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            if (icon != null)
            {
                toggleButton.Add(new Image { image = EditorGUIUtility.LoadIcon(icon) });
                toggleButton.Add(new Label(mainButtonLabel));
            }
            else
            {
                toggleButton.text = mainButtonLabel;
            }

            Add(toggleButton); 
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
    }
}
