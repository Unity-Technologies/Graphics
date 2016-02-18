using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal abstract class VFXEdNodeBlock : CanvasElement
    {
        public string name{ get { return m_Name; } }
        protected string m_Name;

        protected VFXEdNodeBlockParameterField[] m_Fields;

        protected VFXEdDataSource m_DataSource;
        private NodeBlockManipulator m_NodeBlockManipulator;

        public VFXEdNodeBlock(VFXEdDataSource dataSource)
        {
            m_DataSource = dataSource;
            translation = Vector3.zero; // zeroed by default, will be relayouted later.
            m_Caps = Capabilities.Normal;

            m_NodeBlockManipulator = new NodeBlockManipulator(this);
            AddManipulator(m_NodeBlockManipulator);
            AddManipulator(new NodeBlockDelete());

        }

        // Retrieve the full height of the block
        protected abstract float GetHeight();

        public override void Layout()
        {
            base.Layout();

            if (collapsed)
            {
                scale = new Vector2(scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);
            }
            else
            {
                scale = new Vector2(scale.x, GetHeight());
            }

            float curY = VFXEditorMetrics.NodeBlockHeaderHeight;

            foreach(VFXEdNodeBlockParameterField field in m_Fields)
            {
                field.translation = new Vector2(0.0f, curY);
                curY += field.scale.y;
            }


        }

        public bool IsSelectedNodeBlock(VFXEdCanvas canvas)
        {
            if (parent is VFXEdNodeBlockContainer)
            {
                return canvas.SelectedNodeBlock == this;
            }
            else
            {
                return false;
            }
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
                m_DataSource.RemoveDataConnectionsTo(anchor);
            }
            ParentCanvas().ReloadData();
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = GetDrawableRect();

            if (parent is VFXEdNodeBlockContainer)
            {
                if (IsSelectedNodeBlock(canvas as VFXEdCanvas))

                    GUI.Box(r, "", VFXEditor.styles.NodeBlockSelected);
                else
                    GUI.Box(r, "", VFXEditor.styles.NodeBlock);
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

