using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal abstract class VFXEdNodeOption : CanvasElement
    {
        public bool Enabled { get { return m_bEnabled; } set { SetEnabled(value); } }
        protected bool m_bEnabled;

        public VFXEdNodeOption(bool defaultvalue) {
            this.scale = new Vector3(32.0f, 32.0f);
            this.MouseDown += ToggleState;
        }

        public void SetEnabled(bool value)
        {
            if (m_bEnabled != value)
            {
                m_bEnabled = value;
                UpdateModel(UpdateType.Update);
                m_Parent.Invalidate();
                m_Parent.Layout();
            }
        }

        private bool ToggleState(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.used)
                return false;

            Rect r = VFXEditor.styles.NodeOption.padding.Add(canvasBoundingRect);
            if (r.Contains(parent.MouseToCanvas(e.mousePosition))) 
            {
                SetEnabled(!m_bEnabled);
                e.Use();
                element.Invalidate();
                return true;
            }

            return false;
        }

        protected abstract Color GetColor();

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
            Rect r = GetDrawableRect();
            if (Enabled)
                GUI.color = GetColor();
            else
                GUI.color = Color.gray;
            GUI.Box(r, "", VFXEditor.styles.NodeOption);
            GUI.color = Color.white;
        }
    }
}
