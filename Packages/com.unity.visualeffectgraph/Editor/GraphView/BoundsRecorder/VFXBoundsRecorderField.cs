using System;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;

namespace UnityEditor.VFX.UI
{
    class VFXBoundsRecorderField : VisualElement, ISelectable
    {
        private Button m_Button;
        private VisualElement m_Divider;
        private VFXView m_View;


        public string text
        {
            get { return m_Button.text; }
            set { m_Button.text = value; }
        }

        private bool m_Selected = false;

        [System.Obsolete("VFXBoundsRecorderFieldUIFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public class VFXBoundsRecorderFieldUIFactory : UxmlFactory<VFXBoundsRecorderField>
        { }

        IVisualElementScheduledItem m_UpdateItem;

        private VFXContextUI m_TiedContext;
        public VFXBoundsRecorderField()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        public VFXContextUI tiedContext => m_TiedContext;

        public void Setup(VFXContextUI initContextUI, VFXView view)
        {
            m_Button = this.Query<Button>("system-button");
            m_Button.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);
            m_Button.clickable.activators.Clear();
            m_Button.style.borderBottomColor =
                m_Button.style.borderTopColor =
                    m_Button.style.borderLeftColor =
                        m_Button.style.borderRightColor = Color.grey * 0.5f;
            m_Divider = this.Query("divider");
            m_TiedContext = initContextUI;
            m_View = view;
            m_TiedContext.onSelectionDelegate += OnTiedContextSelection;
        }

        public void OnTiedContextSelection(bool tiedContextSelected)
        {
            var selector = GetFirstAncestorOfType<VFXBoundsSelector>();

            if (tiedContextSelected && !m_Selected)
            {
                Select(selector, true);
            }
            if (!tiedContextSelected && m_Selected)
            {
                Unselect(selector);
            }
        }

        public void OnSelected()
        {
            if (!m_Selected)
            {
                if (enabledSelf)
                {
                    m_Selected = true;
                    UpdateBorder();
                    if (!tiedContext.selected)
                        m_View.AddToSelection(tiedContext);
                }
            }
        }

        public void OnUnselected()
        {
            if (m_Selected)
            {
                m_Selected = false;
                UpdateBorder();
                if (tiedContext.selected)
                    m_View.RemoveFromSelection(tiedContext);
            }
        }

        public bool Unselect()
        {
            if (m_Selected)
            {
                var selector = GetFirstAncestorOfType<VFXBoundsSelector>();
                Unselect(selector);
                return true;
            }
            return false;
        }

        void UpdateBorder()
        {
            m_Button.style.borderBottomColor =
                m_Button.style.borderTopColor =
                    m_Button.style.borderLeftColor =
                        m_Button.style.borderRightColor = m_Selected ? new Color(68.0f / 255.0f, 192.0f / 255.0f, 255.0f / 255.0f, 1.0f) : Color.grey * 0.5f;
        }

        void OnMouseDown(MouseDownEvent e)
        {
            var selector = GetFirstAncestorOfType<VFXBoundsSelector>();
            if (IsSelected(selector))
            {
                if (e.actionKey)
                {
                    Unselect(selector);
                }
            }
            else
            {
                Select(selector, e.actionKey);
            }
            e.StopPropagation();
        }

        public bool IsSelectable()
        {
            return true;
        }

        public bool HitTest(Vector2 localPoint)
        {
            return ContainsPoint(localPoint);
        }

        public void Select(VisualElement selectionContainer, bool additive)
        {
            if (selectionContainer is ISelection selection)
            {
                if (!selection.selection.Contains(this))
                {
                    if (!additive)
                    {
                        selection.ClearSelection();
                        selection.AddToSelection(this);
                    }
                    else
                    {
                        selection.AddToSelection(this);
                    }
                }
            }
        }

        public void Unselect(VisualElement selectionContainer)
        {
            if (selectionContainer is ISelection selection)
            {
                if (selection.selection.Contains(this))
                {
                    selection.RemoveFromSelection(this);
                }
            }
        }

        public bool IsSelected(VisualElement selectionContainer)
        {
            if (selectionContainer is ISelection selection)
            {
                if (selection.selection.Contains(this))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
