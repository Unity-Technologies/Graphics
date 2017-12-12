using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXFlowAnchorController : Controller, IVFXAnchorController
    {
        [SerializeField]
        VFXContextController m_Context;
        public VFXContext owner { get { return m_Context.context; } }
        public VFXContextController context { get { return m_Context; } }

        [SerializeField]
        private int m_SlotIndex;
        public int slotIndex { get { return m_SlotIndex; } }

        public void Init(VFXContextController context, int slotIndex)
        {
            m_Context = context;
            m_SlotIndex = slotIndex;
        }

        List<VFXFlowEdgePresenter> m_Connections = new List<VFXFlowEdgePresenter>();

        public virtual void Connect(VFXEdgeController edgePresenter)
        {
            m_Connections.Add(edgePresenter as VFXFlowEdgePresenter);
        }

        public virtual void Disconnect(VFXEdgeController edgePresenter)
        {
            m_Connections.Remove(edgePresenter as VFXFlowEdgePresenter);
        }

        public bool connected
        {
            get { return m_Connections.Count > 0; }
        }

        public virtual bool IsConnectable()
        {
            return true;
        }

        public abstract Direction direction { get; }
        public Orientation orientation { get { return Orientation.Vertical; } }

        public IEnumerable<VFXFlowEdgePresenter> connections { get { return m_Connections; } }

        public override void ApplyChanges()
        {
        }
    }

    class VFXFlowInputAnchorController : VFXFlowAnchorController
    {
        public VFXFlowInputAnchorController()
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
        public override bool IsConnectable()
        {
            return !connected;
        }
    }

    class VFXFlowOutputAnchorController : VFXFlowAnchorController
    {
        public VFXFlowOutputAnchorController()
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Output;
            }
        }
    }
}
