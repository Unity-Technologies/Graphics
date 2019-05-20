using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Position))]
    class VFXSlotPosition : VFXSlotEncapsulated
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Vector3>(Vector3.zero, mode);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || VFXSlotFloat3.CanConvertFromVector3(type);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            return VFXSlotFloat3.ConvertExpressionToVector3(expression);
        }
    }
}
