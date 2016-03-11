using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNodeBlockDraggable : VFXEdNodeBlock
    {
        private NodeBlockManipulator m_NodeBlockManipulator;

        public VFXEdNodeBlockDraggable(VFXEdDataSource dataSource) : base(dataSource)
        {
            m_NodeBlockManipulator = new NodeBlockManipulator(this);
            AddManipulator(m_NodeBlockManipulator);
            AddManipulator(new NodeBlockDelete());
        }

        public virtual void OnRemoved()
        {
            List<VFXEdDataAnchor> anchors = new List<VFXEdDataAnchor>();
            foreach(VFXEdNodeBlockParameterField field in m_Fields)
            {
                if (field.Input != null)
                    anchors.Add(field.Input);
                if (field.Output != null)
                    anchors.Add(field.Output);
            }
            foreach(VFXEdDataAnchor anchor in anchors)
            {
                m_DataSource.RemoveConnectedEdges<DataEdge, VFXEdDataAnchor>(anchor);
            }
            ParentCanvas().ReloadData();
        }

        protected override GUIStyle GetNodeBlockStyle()
        {
            return VFXEditor.styles.DataNodeBlock;
        }

        protected override GUIStyle GetNodeBlockSelectedStyle()
        {
            return VFXEditor.styles.DataNodeBlockSelected;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = GetDrawableRect();

            if (parent is VFXEdNodeBlockContainer)
            {
                if (IsSelectedNodeBlock(canvas as VFXEdCanvas))

                    GUI.Box(r, "", GetNodeBlockSelectedStyle());
                else
                    GUI.Box(r, "", GetNodeBlockStyle());
            }
            else // If currently dragged...
            {
                Color c = GUI.color;
                GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.a, 0.75f);
                GUI.Box(r, "", VFXEditor.styles.NodeBlockSelected);
                GUI.color = c;
            }


            base.Render(parentRect, canvas);
        }

    }
}
