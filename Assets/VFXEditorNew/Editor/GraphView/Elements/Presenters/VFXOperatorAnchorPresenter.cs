using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorAnchorPresenter : NodeAnchorPresenter
    {
        public void Init(VFXOperatorPresenter source, Guid slotId, Direction direction)
        {
            m_SourceOperator = source;
            m_Direction = direction;
            m_SlotID = slotId;
        }

        private VFXOperatorPresenter m_SourceOperator;
        private Direction m_Direction;
        private Guid m_SlotID;

        public override Direction direction
        {
            get
            {
                return m_Direction;
            }
        }

        public VFXOperatorPresenter sourceOperator
        {
            get
            {
                return m_SourceOperator;
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