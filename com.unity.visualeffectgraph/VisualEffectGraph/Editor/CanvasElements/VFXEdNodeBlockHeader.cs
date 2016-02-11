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
        public bool Highlight
        {
            get { return m_Highlight; }
            set { m_Highlight = value; }
        }
        private bool m_Highlight;

        private NodeBlockCollapse m_NodeBlockCollapseManipulator;
        private string m_Name;

        public VFXEdNodeBlockHeader(float width, VFXEdDataSource dataSource, string Text)
        {
            translation = Vector3.zero;
            this.scale = new Vector2(width, VFXEditorMetrics.NodeBlockHeaderHeight);
            m_Name = Text;
            scale = new Vector2(width, VFXEditorMetrics.NodeBlockHeaderHeight);

            m_NodeBlockCollapseManipulator = new NodeBlockCollapse();
            AddManipulator(m_NodeBlockCollapseManipulator);
        }

        public override void Layout()
        {
            scale = new Vector2(this.scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);
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

            if (Highlight)
            {
                EditorGUI.DrawRect(drawablerect, new Color(1.0f, 1.0f, 1.0f, 0.2f));
            }
            else
            {
                GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.75f);
            }


            if (collapsed)
            {
                GUI.Box(arrowrect, "", VFXEditor.styles.CollapserClosed);
            }
            else
            {
                GUI.Box(arrowrect, "", VFXEditor.styles.CollapserOpen);
            }


            if (!Highlight)
                GUI.color = Color.white;

            GUI.Label(labelrect, m_Name, VFXEditor.styles.NodeBlockTitle);
            base.Render(parentRect, canvas);
        }

    }
}

