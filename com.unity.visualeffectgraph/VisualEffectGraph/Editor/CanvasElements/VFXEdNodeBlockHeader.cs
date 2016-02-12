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
        private Texture2D m_Icon;

        public VFXEdNodeBlockHeader(VFXEdDataSource dataSource, string Text,Texture2D icon,  bool Collapseable)
        {
            translation = Vector3.zero;
            scale = new Vector2(100, VFXEditorMetrics.NodeBlockHeaderHeight);
            m_Icon = icon;
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

            Rect arrowrect = VFXEditorMetrics.NodeBlockHeaderFoldoutRect;
            arrowrect.min = arrowrect.min + drawablerect.min;
            arrowrect.size = VFXEditorMetrics.NodeBlockHeaderFoldoutRect.size;

            Rect iconrect = VFXEditorMetrics.NodeBlockHeaderIconRect;
            iconrect.min = iconrect.min + drawablerect.min;
            iconrect.size = VFXEditorMetrics.NodeBlockHeaderIconRect.size;

            Rect labelrect = drawablerect;
            labelrect.min += VFXEditorMetrics.NodeBlockHeaderLabelPosition;


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
            GUI.Box(iconrect, m_Icon, VFXEditor.styles.Empty);
            GUI.Label(labelrect, m_Name, VFXEditor.styles.NodeBlockTitle);
            base.Render(parentRect, canvas);
        }

    }
}

