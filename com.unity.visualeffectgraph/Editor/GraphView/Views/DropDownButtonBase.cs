using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class DropDownButtonBase : VisualElement
    {
        protected readonly VFXView m_VFXView;

        private class NotifyEditorWindow : EditorWindow
        {
            public event Action Destroyed;

            private void OnDestroy()
            {
                Destroyed?.Invoke();
            }
        }

        readonly bool m_HasLeftSeparator;
        readonly Button m_MainButton;

        NotifyEditorWindow m_CurrentPopup;
        DateTime m_PopupClosedTimestamp;

        protected readonly VisualElement m_PopupContent;

        protected DropDownButtonBase(
            VFXView view,
            string uxmlSource,
            string mainButtonLabel,
            string mainButtonName,
            string iconPath,
            bool hasSeparatorBefore = false,
            bool hasSeparatorAfter = false)
        {
            m_VFXView = view;
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
                var icon = new Image { image = EditorGUIUtility.LoadIcon(iconPath) };
                m_MainButton.Add(icon);
                m_MainButton.tooltip = mainButtonLabel;
            }
            else
            {
                m_MainButton.text = mainButtonLabel;
            }
            Add(m_MainButton);

            var separator = new VisualElement();
            separator.AddToClassList("dropdown-separator");
            Add(separator);

            var dropDownButton = new Button(OnTogglePopup);
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

        protected virtual void OnOpenPopup() { }
        protected virtual void OnMainButton() { }
        protected abstract Vector2 GetPopupSize();

        protected void ClosePopup()
        {
            m_CurrentPopup?.Close();
        }

        private Vector2 GetPopupPosition() => m_VFXView.ViewToScreenPosition(worldBound.position);

        private void OnTogglePopup()
        {
            // If the user click on the arrow button while the popup is opened
            // the popup is then closed (because clicked outside) and immediately reopened
            // To prevent this behavior we allow the popup to reopen only after a very short period of time after being closed
            var deltaTime = DateTime.UtcNow - m_PopupClosedTimestamp;
            if (m_CurrentPopup == null && deltaTime.TotalMilliseconds > 500)
            {
                m_CurrentPopup = ScriptableObject.CreateInstance<NotifyEditorWindow>();
                m_CurrentPopup.hideFlags = HideFlags.HideAndDontSave;
                if (m_PopupContent.parent != null)
                {
                    m_PopupContent.parent.Remove(m_PopupContent);
                }

                m_CurrentPopup.Destroyed += OnPopupClosed;
                m_CurrentPopup.rootVisualElement.AddStyleSheetPath("VFXToolbar");
                m_CurrentPopup.rootVisualElement.Add(m_PopupContent);
                m_CurrentPopup.rootVisualElement.AddToClassList("popup");
                m_CurrentPopup.rootVisualElement.RegisterCallback<KeyUpEvent>(OnKeyUp);

                OnOpenPopup();
                var bounds = new Rect(GetPopupPosition(), localBound.size);
                // Offset the bounds to align the popup with the real dropdown left edge
                if (m_HasLeftSeparator)
                {
                    bounds.xMin += 6;
                }

                m_CurrentPopup.ShowAsDropDown(bounds, GetPopupSize(), new[] { PopupLocation.BelowAlignLeft, PopupLocation.AboveAlignLeft });
                m_CurrentPopup.minSize = GetPopupSize();
                m_CurrentPopup.maxSize = m_CurrentPopup.minSize;
                GetNextFocusable(null, m_PopupContent.Children(), false)?.Focus();
            }
            else if (m_CurrentPopup != null)
            {
                ClosePopup();
            }
        }

        private void OnPopupClosed()
        {
            m_CurrentPopup.Destroyed -= OnPopupClosed;
            m_CurrentPopup.rootVisualElement.UnregisterCallback<KeyUpEvent>(OnKeyUp);
            m_CurrentPopup = null;
            m_PopupClosedTimestamp = DateTime.UtcNow;
        }

        private void OnKeyUp(KeyUpEvent evt)
        {
            var focused = m_PopupContent.focusController.focusedElement;
            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    var next = GetNextFocusable(focused, m_PopupContent.Children(), false);
                    next?.Focus();
                    break;
                case KeyCode.UpArrow:
                    var prev = GetNextFocusable(focused, m_PopupContent.Children(), true);
                    prev?.Focus();
                    break;
                case KeyCode.Escape:
                    ClosePopup();
                    break;
            }
        }

        private VisualElement GetNextFocusable(Focusable focused, IEnumerable<VisualElement> elements, bool reverse)
        {
            var found = focused == null;
            return GetNextFocusableRecursive(focused, elements, reverse, ref found);
        }

        private VisualElement GetNextFocusableRecursive(Focusable focused, IEnumerable<VisualElement> elements, bool reverse, ref bool found)
        {
            var collection = reverse ? elements.Reverse() : elements;
            foreach (var child in collection)
            {
                if (child == focused)
                {
                    found = true;
                    continue;
                }

                if (found && child != focused && child.enabledSelf && child.focusable)
                {
                    return child;
                }

                var next = GetNextFocusableRecursive(focused, child.Children(), reverse, ref found);
                if (next != null)
                {
                    return next;
                }
            }

            return null;
        }
    }
}
