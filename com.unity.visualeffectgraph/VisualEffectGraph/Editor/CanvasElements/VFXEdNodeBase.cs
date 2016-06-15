using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal abstract class VFXEdNodeBase : CanvasElement
    {

        public static int Token { get { return s_Token++; } }
        private static int s_Token = 0;

        public string UniqueName { get { return m_UniqueName; } }

        public List<VFXEdFlowAnchor> inputs
        {
            get { return m_Inputs; }
        }

        public List<VFXEdFlowAnchor> outputs
        {
            get { return m_Outputs; }
        }

        public VFXEdDataSource DataSource { get { return m_DataSource; } }

        protected string m_UniqueName;

        protected List<VFXEdFlowAnchor> m_Inputs;
        protected List<VFXEdFlowAnchor> m_Outputs;
        protected VFXEdDataSource m_DataSource;
        protected Rect m_ClientArea;

        internal VFXEdNodeBase(Vector2 canvasposition, VFXEdDataSource datasource, bool deletable = true) : base () {
            m_DataSource = datasource;
            translation = canvasposition;
            m_Inputs = new List<VFXEdFlowAnchor>();
            m_Outputs = new List<VFXEdFlowAnchor>();


            m_ClientArea = new Rect(0, 0, scale.x, scale.y);

            m_UniqueName = GetType().Name + "_" + Token;

            AddManipulator(new GridSnapDraggable(10.0f));

            if (deletable)
                AddManipulator(new NodeDelete());
        }

        //public abstract void OnRemove();

    }
}
