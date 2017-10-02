using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXBuiltInParameter : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        [SerializeField]
        private VFXExpressionOp m_expressionOp;

        public override T Clone<T>()
        {
            var copy = base.Clone<T>() as VFXBuiltInParameter;
            copy.Init(m_expressionOp);
            return copy as T;
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties { get { return PropertiesFromSlotsOrDefaultFromClass(VFXSlot.Direction.kOutput); } }

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
                AddSlot(VFXSlot.Create(new VFXProperty(VFXExpression.TypeToType(exp.valueType), "o"), VFXSlot.Direction.kOutput));
            }
            outputSlots[0].SetExpression(exp);
        }
    }
}
