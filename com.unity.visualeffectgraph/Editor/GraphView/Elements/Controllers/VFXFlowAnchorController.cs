using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.VFX;

namespace UnityEditor.VFX.UI
{
    abstract class VFXFlowAnchorController : Controller, IVFXAnchorController
    {
        VFXContextController m_Context;
        public VFXContext owner { get { return m_Context.model; } }
        public VFXContextController context { get { return m_Context; } }

        private int m_SlotIndex;
        public int slotIndex { get { return m_SlotIndex; } }

        public void Init(VFXContextController context, int slotIndex)
        {
            m_Context = context;
            m_SlotIndex = slotIndex;
        }

        List<VFXFlowEdgeController> m_Connections = new List<VFXFlowEdgeController>();

        public virtual void Connect(VFXEdgeController edgeController)
        {
            m_Connections.Add(edgeController as VFXFlowEdgeController);
        }

        public virtual void Disconnect(VFXEdgeController edgeController)
        {
            m_Connections.Remove(edgeController as VFXFlowEdgeController);
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

        public IEnumerable<VFXFlowEdgeController> connections { get { return m_Connections; } }

        public override void ApplyChanges()
        {
        }

        public virtual string title
        {
            get { return ""; }
        }

        static private IEnumerable<VFXContext> GetFlowAncestor(VFXContext from)
        {
            yield return from;
            foreach (var flowInput in from.inputFlowSlot)
            {
                foreach (var context in flowInput.link.Select(o => o.context).Where(o => o != null))
                {
                    var ancestors = GetFlowAncestor(context);
                    foreach (var ancestor in ancestors)
                    {
                        yield return ancestor;
                    }
                }
            }
        }

        static public bool CanLink(VFXFlowAnchorController from, VFXFlowAnchorController to)
        {
            var flowAncestor = GetFlowAncestor(from.owner);
            if (flowAncestor.Contains(to.owner))
                return false; //Avoid loop in graph

            return VFXContext.CanLink(from.owner, to.owner, from.slotIndex, to.slotIndex);
        }
    }

    class VFXFlowInputAnchorController : VFXFlowAnchorController
    {
        public VFXFlowInputAnchorController()
        {
        }

        public override string title
        {
            get
            {
                if (owner is VFXBasicSpawner)
                {
                    switch (slotIndex)
                    {
                        case 0:
                            return "Start";
                        case 1:
                            return "Stop";
                    }
                }
                else if (owner is VFXSubgraphContext)
                {
                    string name = (owner as VFXSubgraphContext).GetInputFlowName(slotIndex);
                    if (slotIndex == 0)
                    {
                        if (name == VisualEffectAsset.PlayEventName)
                            return "Start";
                        else if (name == VisualEffectAsset.StopEventName)
                            return "Stop";
                    }
                    else if (slotIndex == 1)
                    {
                        if (name == VisualEffectAsset.StopEventName)
                            return "Stop";
                    }
                    return name;
                }
                return "";
            }
        }

        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
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
