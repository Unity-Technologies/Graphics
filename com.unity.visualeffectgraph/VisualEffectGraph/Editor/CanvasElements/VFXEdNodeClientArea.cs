using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdNodeClientArea : CanvasElement
    {

        public VFXEdNodeBlockContainer NodeBlockContainer
        { get { return m_NodeBlockContainer; } }
        private VFXEdNodeBlockContainer m_NodeBlockContainer;

        public VFXEdNodeClientArea(Rect clientRect, VFXEdDataSource dataSource)
        {
            translation = VFXEditorMetrics.NodeClientAreaPosition;
            translation = clientRect.position;
            scale = new Vector2(clientRect.width, clientRect.height);
            m_Caps = Capabilities.Normal;
            m_NodeBlockContainer = new VFXEdNodeBlockContainer(clientRect.size, dataSource);
            AddChild(m_NodeBlockContainer);
        }


        public override void Layout()
        {
            base.Layout();
            scale = new Vector2(scale.x, m_NodeBlockContainer.scale.y + 41);
        }


        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = GetDrawableRect();

            if (FindParent<VFXEdDataNode>() == null)
            {
                GUI.Box(r, "", VFXEditor.styles.Node);

                GUI.Label(new Rect(0, r.y, r.width, 24), FindParent<VFXEdNode>().title, VFXEditor.styles.NodeTitle);

            } else {

                VFXEdDataNode n = FindParent<VFXEdDataNode>();
                if(n.exposed)
                {
                    GUI.Box(r, "", VFXEditor.styles.NodeParameters);
                    GUI.Label(new Rect(0, r.y, r.width, 24), "Parameter Interface", VFXEditor.styles.NodeParametersTitle);
                }
                else
                {
                    GUI.Box(r, "", VFXEditor.styles.NodeData);
                    GUI.Label(new Rect(0, r.y, r.width, 24), "Local Constants", VFXEditor.styles.NodeParametersTitle);
                }
            }

            if (selected)
                    GUI.Box(r, "", VFXEditor.styles.NodeSelected);

            base.Render(parentRect, canvas);
        }

    }
}

