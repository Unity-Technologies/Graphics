using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXFlowAnchorPresenter : PortPresenter
    {
        [SerializeField]
        VFXContextPresenter m_Context;
        public VFXContext owner { get { return m_Context.context; } }
        public VFXContextPresenter context { get { return m_Context; } }

        [SerializeField]
        private int m_SlotIndex;
        public int slotIndex { get { return m_SlotIndex; } }

        public void Init(VFXContextPresenter context, int slotIndex)
        {
            m_Context = context;
            m_SlotIndex = slotIndex;
            portType = typeof(int); // We dont care about that atm!
            orientation = Orientation.Vertical;
        }
    }

    class VFXFlowInputAnchorPresenter : VFXFlowAnchorPresenter
    {
        public VFXFlowInputAnchorPresenter()
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXFlowOutputAnchorPresenter : VFXFlowAnchorPresenter
    {
        public VFXFlowOutputAnchorPresenter()
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
