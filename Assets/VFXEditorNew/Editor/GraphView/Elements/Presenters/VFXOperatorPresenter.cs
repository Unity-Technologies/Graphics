using RMGUI.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorPresenter : NodePresenter, IVFXPresenter
    {
        [SerializeField]
        private VFXOperator m_Operator;
        public VFXOperator Operator     { get { return m_Operator; } }
        public virtual VFXModel model   { get { return m_Operator; } }


        private NodeAnchorPresenter CreateAnchorPresenter(VFXOperator.VFXMitoSlot slot, Direction direction)
        {
            var inAnchor = CreateInstance<VFXOperatorAnchorPresenter>();
            inAnchor.anchorType = slot.type;
            inAnchor.name = slot.name;
            inAnchor.Init(this, slot.slotID, direction);
            return inAnchor;
        }

        public virtual void Init(VFXModel model,VFXViewPresenter viewPresenter)
        {
            m_Operator = (VFXOperator)model;

            title = m_Operator.name;

            inputAnchors.Clear();
            outputAnchors.Clear();

            inputAnchors.AddRange(m_Operator.InputSlots.Select(s => CreateAnchorPresenter(s, Direction.Input)));
            outputAnchors.AddRange(m_Operator.OutputSlots.Select(s => CreateAnchorPresenter(s, Direction.Output)));
        }
    }
}
