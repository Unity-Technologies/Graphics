using System;
using UnityEngine;
//using Direction = UnityEditor.VFX.Direction;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(DirectionType))]
    class VFXSlotDirection : VFXSlot
    {
        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Vector3>(Vector3.forward, mode);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return type == typeof(Vector3);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return VFXOperatorUtility.Normalize(expr[0]);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[1] { expr };
        }
    }
}
