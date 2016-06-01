using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNodeBlockHeaderSimple : VFXEdNodeBlockHeader
    {

        private string m_Name;

        internal VFXEdNodeBlockHeaderSimple(string Text, Texture2D icon, bool Collapseable)
            : base(icon, Collapseable, true)
        {
            m_Name = Text;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            Rect drawablerect = GetDrawableRect();

            Rect labelrect = drawablerect;
            labelrect.min += VFXEditorMetrics.NodeBlockHeaderLabelPosition;

            GUI.Label(labelrect, m_Name, VFXEditor.styles.NodeBlockTitle);
        }
    }
}
