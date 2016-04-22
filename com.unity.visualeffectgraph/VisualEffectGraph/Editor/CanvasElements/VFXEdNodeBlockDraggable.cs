using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal abstract class VFXEdNodeBlockDraggable : VFXEdNodeBlock
    {

        public VFXEdNodeBlockDraggable(VFXEdDataSource dataSource) : base(dataSource)
        {
            AddManipulator(new NodeBlockManipulator(this));
            AddManipulator(new NodeBlockDelete());
        }

        public virtual void OnRemoved()
        {
            List<VFXUIPropertyAnchor> anchors = new List<VFXUIPropertyAnchor>();
            foreach (var field in m_Fields)
                if (field.Anchor != null)
                    anchors.Add(field.Anchor);

            foreach (var anchor in anchors)
                m_DataSource.RemoveConnectedEdges<VFXUIPropertyEdge, VFXUIPropertyAnchor>(anchor);

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
