using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXNodeAnchorPresenter : NodeAnchorPresenter
    {
        public void Init(VFXNodePresenter source, Guid slotId, Direction direction)
        {
            m_SourceNode = source;
            m_Direction = direction;
            m_SlotID = slotId;
        }

        //Ignore m_Connection, this information is store in model
        public override void Connect(EdgePresenter edgePresenter) {}
        public override void Disconnect(EdgePresenter edgePresenter) {}

        private VFXNodePresenter m_SourceNode;
        private Direction m_Direction;
        private Guid m_SlotID;

        public override Direction direction
        {
            get
            {
                return m_Direction;
            }
        }

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
}