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

        public List<VFXEdFlowAnchor> inputs
        {
            get { return m_Inputs; }
        }

        public List<VFXEdFlowAnchor> outputs
        {
            get { return m_Outputs; }
        }

        protected List<VFXEdFlowAnchor> m_Inputs;
        protected List<VFXEdFlowAnchor> m_Outputs;
        protected VFXEdDataSource m_DataSource;
        protected Rect m_ClientArea;

        internal VFXEdNodeBase(Vector2 canvasposition, VFXEdDataSource datasource) : base () {
            m_DataSource = datasource;
            translation = canvasposition;
            m_Inputs = new List<VFXEdFlowAnchor>();
            m_Outputs = new List<VFXEdFlowAnchor>();

            m_ClientArea = new Rect(0, 0, scale.x, scale.y);

            AddManipulator(new Draggable());
            AddManipulator(new NodeDelete());

        }

        public abstract void OnRemove();

    }
}
