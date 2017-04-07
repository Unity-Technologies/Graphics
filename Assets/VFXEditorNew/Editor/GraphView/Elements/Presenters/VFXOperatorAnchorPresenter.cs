using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    abstract class VFXOperatorAnchorPresenter : VFXDataAnchorPresenter
    {
        public void Init(VFXModel owner, VFXSlot model, VFXNodePresenter source, Guid slotId)
        {
            base.Init(owner, model);
            m_SourceNode = source;
            m_SlotID = slotId;
        }

        //Ignore m_Connection, this information is store in model
        public override void Connect(EdgePresenter edgePresenter) {}
        public override void Disconnect(EdgePresenter edgePresenter) {}

        private VFXNodePresenter m_SourceNode;
        private Guid m_SlotID;

        public VFXNodePresenter sourceNode
        {
            get
            {
                return m_SourceNode;
            }
        }

        public Guid slotID
        {
            get
            {
                return m_SlotID;
            }
        }
    }


    class VFXInputOperatorAnchorPresenter : VFXOperatorAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXOutputOperatorAnchorPresenter : VFXOperatorAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Output;
            }
        }
    }
}