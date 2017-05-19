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
            m_expressionOp = op;
            var exp = VFXBuiltInExpression.Find(m_expressionOp);
            if (outputSlots.Count == 0)
            {
                AddSlot(VFXSlot.Create(new VFXProperty(VFXExpression.TypeToType(exp.ValueType), "o"), VFXSlot.Direction.kOutput));
            }
            outputSlots[0].SetExpression(exp);
        }
    }
}
