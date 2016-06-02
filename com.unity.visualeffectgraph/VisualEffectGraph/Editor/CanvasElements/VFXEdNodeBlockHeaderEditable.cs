using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using UnityEditor.Experimental.VFX;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNodeBlockHeaderEditable : VFXEdNodeBlockHeader
    {
        private VFXDataBlockModel m_Model;

        internal VFXEdNodeBlockHeaderEditable(VFXDataBlockModel model, Texture2D Icon, bool Collapseable) 
            : base(Icon, Collapseable, false)
        {
            m_Model = model;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            Rect drawablerect = GetDrawableRect();

            Rect labelrect = drawablerect;
            labelrect.min += VFXEditorMetrics.NodeBlockHeaderLabelPosition;

            m_Model.ExposedName = GUI.TextField(labelrect, m_Model.ExposedName, VFXEditor.styles.NodeBlockTitleEditable);
        }
    }
}
