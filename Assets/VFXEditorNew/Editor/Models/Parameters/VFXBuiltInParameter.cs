using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXBuiltInParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        [SerializeField]
        private VFXExpressionOp m_expressionOp;

        public VFXExpressionOp expressionOp
        {
            get
            {
                return m_expressionOp;
            }
        }

        public VFXBuiltInParameter()
        {
            m_expressionOp = VFXExpressionOp.kVFXNoneOp;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (m_expressionOp != VFXExpressionOp.kVFXNoneOp)
            {
                Init(m_expressionOp);
            }
        }

        public void Init(VFXExpressionOp op)
        {
            while (outputSlots.Count > 0)
            {
                RemoveSlot(outputSlots[0]);
            }
            m_expressionOp = op;
            var exp = VFXBuiltInExpression.Find(m_expressionOp);
            AddSlot(VFXSlot.Create(new VFXProperty(VFXExpression.TypeToType(exp.ValueType), "o"), VFXSlot.Direction.kOutput));
            outputSlots[0].SetExpression(exp);
        }
    }
}