using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Position))]
    class VFXSlotPosition : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Vector3>(Vector3.zero, mode);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(Vector4) ||  type == typeof(Vector3);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expresssion, VFXSlot sourceSlot)
        {
            if (expresssion.valueType == VFXValueType.Float3)
                return expresssion;

            return VFXOperatorUtility.CastFloat(expresssion, VFXValueType.Float3);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return expr[0];
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[1] { expr };
        }
    }
}
