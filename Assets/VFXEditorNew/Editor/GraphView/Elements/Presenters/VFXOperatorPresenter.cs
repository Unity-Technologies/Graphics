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

        [SerializeField]
        public int m_dirtyHack;

        [SerializeField]
        private object m_settings;
        public object settings { get { return m_settings; } set { m_settings = value; m_dirtyHack++; } }

        private NodeAnchorPresenter CreateAnchorPresenter(VFXOperator.VFXMitoSlot slot, Direction direction)
        {
            var inAnchor = CreateInstance<VFXOperatorAnchorPresenter>();
            var expression = slot.expression;
            inAnchor.anchorType = expression == null ? typeof(float) : VFXExpression.TypeToType(expression.ValueType);
            if (expression == null)
            {
                inAnchor.name = "Empty";
            }

            inAnchor.Init(this, slot.slotID, direction);
            return inAnchor;
        }

        public void Reset()
        {
            inputAnchors.Clear();
            outputAnchors.Clear();

            if (m_Operator != null)
            {
                position = new Rect(model.position.x, model.position.y, position.width, position.height);
                expanded = !model.collapsed;
                title = Operator.name;
                settings = Operator.settings;
                inputAnchors.AddRange(Operator.InputSlots.Select(s => CreateAnchorPresenter(s, Direction.Input)));
                outputAnchors.AddRange(Operator.OutputSlots.Select(s => CreateAnchorPresenter(s, Direction.Output)));
            }
        }

        public virtual void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            m_Operator = (VFXOperator)model;
            Reset();
        }
    }
}
