using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXFlowAnchorPresenter : NodeAnchorPresenter
    {
        [SerializeField]
        private VFXContext m_Owner;
        public VFXContext Owner { get { return m_Owner; } }

        [SerializeField]
        private int m_SlotIndex;
        public int slotIndex { get { return m_SlotIndex; } }

        public void Init(VFXContext owner, int slotIndex)
        {
            m_Owner = owner;
            m_SlotIndex = slotIndex;
            anchorType = typeof(int); // We dont care about that atm!
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
