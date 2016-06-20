using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNodeBlockHeader : CanvasElement
    {
        public bool Collapseable { get { return m_NodeBlockCollapseManipulator.Enabled; } set { m_NodeBlockCollapseManipulator.Enabled = value; } }

        private Collapsable m_NodeBlockCollapseManipulator;
        private Texture2D m_Icon;

        public VFXEdNodeBlockHeader(Texture2D icon, bool Collapseable, bool bUseDoubleClick)
        {
            translation = Vector3.zero;
            scale = new Vector2(100, VFXEditorMetrics.NodeBlockHeaderHeight);
            m_Icon = icon;
            m_NodeBlockCollapseManipulator = new NodeBlockCollapse(Collapseable, bUseDoubleClick);
            AddManipulator(m_NodeBlockCollapseManipulator);

        }


        public override void Layout()
        {
            scale = new Vector2(parent.scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);
            base.Layout();
        }


        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            
            Rect drawablerect = GetDrawableRect();

            Rect arrowrect = VFXEditorMetrics.NodeBlockHeaderFoldoutRect;
            arrowrect.min = arrowrect.min + drawablerect.min;
            arrowrect.size = VFXEditorMetrics.NodeBlockHeaderFoldoutRect.size;

            Rect iconrect = VFXEditorMetrics.NodeBlockHeaderIconRect;
            iconrect.min = iconrect.min + drawablerect.min;
            iconrect.size = VFXEditorMetrics.NodeBlockHeaderIconRect.size;


            if (Collapseable)
            {
                if (collapsed)
                {
                    GUI.Box(arrowrect, "", VFXEditor.styles.CollapserClosed);
                }
                else
                {
                    GUI.Box(arrowrect, "", VFXEditor.styles.CollapserOpen);
                }
            }
            else
            {
                GUI.Box(arrowrect, "", VFXEditor.styles.CollapserDisabled);
            }

            if (m_Icon != null)
                GUI.DrawTexture(iconrect, m_Icon);
            base.Render(parentRect, canvas);
        }

    }
}

