using System;
using UnityEngine;
//using Direction = UnityEditor.VFX.Direction;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(DirectionType))]
    class VFXSlotDirection : VFXSlot
    {
        /*protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || (type == typeof(Vector3));
        }*/

        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Vector3>(Vector3.forward, true);
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
