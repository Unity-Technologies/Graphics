using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdDataNode : VFXEdNode
    {
        public bool exposed { get { return m_Exposed; } }
        protected bool m_Exposed;

        internal VFXEdDataNode(Vector2 canvasposition, VFXEdDataSource dataSource) 
            : base (canvasposition, dataSource)
        {
            m_Title = "Data Node";
            this.AddManipulator(new ImguiContainer());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = m_ClientArea;

            if(exposed)
            {
                GUI.Box(r, "", VFXEditor.styles.NodeParameters);
                GUI.Label(new Rect(r.x, r.y, r.width, 24), "Parameter Interface", VFXEditor.styles.NodeParametersTitle);
            }
            else
            {
                GUI.Box(r, "", VFXEditor.styles.NodeData);
                GUI.Label(new Rect(r.x, r.y, r.width, 24), "Local Constants", VFXEditor.styles.NodeParametersTitle);
            }  

            base.Render(parentRect, canvas);
            m_Exposed = GUI.Toggle(new Rect(r.x+12, r.y+8, 16, 24), m_Exposed, "");
        }
    }
}
