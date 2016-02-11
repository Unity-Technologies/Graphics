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

        private bool m_Collapseable;

        private NodeBlockCollapse m_NodeBlockCollapseManipulator;
        private string m_Name;

        public VFXEdNodeBlockHeader(VFXEdDataSource dataSource, string Text, bool Collapseable)
        {
            translation = Vector3.zero;
            scale = new Vector2(100, VFXEditorMetrics.NodeBlockHeaderHeight);

            m_Name = Text;

            m_Collapseable = Collapseable;

            if (m_Collapseable)
            {
                m_NodeBlockCollapseManipulator = new NodeBlockCollapse();
                AddManipulator(m_NodeBlockCollapseManipulator);
            }

        }


        public override void Layout()
        {
            scale = new Vector2(parent.scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);
            base.Layout();
        }


        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect drawablerect = GetDrawableRect();

            Rect arrowrect = VFXEditorMetrics.NodeBlockCollapserArrowRect;
            arrowrect.min = arrowrect.min + drawablerect.min;
            arrowrect.size = VFXEditorMetrics.NodeBlockCollapserArrowRect.size;

            Rect labelrect = drawablerect;
            labelrect.min += VFXEditorMetrics.NodeBlockCollapserLabelPosition;

            if (m_Collapseable)
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

            GUI.Label(labelrect, m_Name, VFXEditor.styles.NodeBlockTitle);
            base.Render(parentRect, canvas);
        }

    }
}

