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
            base.Render(parentRect, canvas);
            m_Exposed = GUI.Toggle(new Rect(32, 32, 16, 24), m_Exposed, "");
        }
    }
}
