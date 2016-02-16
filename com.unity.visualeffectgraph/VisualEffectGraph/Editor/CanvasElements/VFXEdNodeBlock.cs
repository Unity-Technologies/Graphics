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

        private NodeBlockManipulator m_NodeBlockManipulator;

        public VFXEdNodeBlock(VFXEdDataSource dataSource)
        {
            
            translation = Vector3.zero; // zeroed by default, will be relayouted later.
            m_Caps = Capabilities.Normal;

            m_NodeBlockManipulator = new NodeBlockManipulator(this);
            AddManipulator(m_NodeBlockManipulator);
            AddManipulator(new NodeBlockDelete());


        }

        // Retrieve the height of a given param
        protected abstract float GetParamHeight(VFXParam param);
        // Retrieve the full height of the block
        protected abstract float GetHeight();

        public override void Layout()
        {
            if (collapsed)
            {
                scale = new Vector2(scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);
            }
            else
            {
                scale = new Vector2(scale.x, GetHeight());
            }

            base.Layout();

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

        public abstract void OnRemoved();

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

