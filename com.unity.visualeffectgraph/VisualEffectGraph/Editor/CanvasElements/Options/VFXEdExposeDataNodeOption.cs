using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdExposeDataNodeOption : VFXEdNodeOption
    {
        private Color m_Color = new Color(1.0f, 0.5f, 0.0f);

        internal VFXEdExposeDataNodeOption() : base(false) {

        }

        protected override Color GetColor()
        {
            return m_Color;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            Rect iconRect = VFXEditor.styles.NodeOption.padding.Remove(GetDrawableRect());
            iconRect = new RectOffset(4, 4, 4, 4).Remove(iconRect);
            GUI.DrawTexture(iconRect, VFXEditor.styles.GetIcon("Config"));
        }
    }
}
