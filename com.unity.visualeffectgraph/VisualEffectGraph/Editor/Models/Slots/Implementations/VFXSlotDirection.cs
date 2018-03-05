using System;
using UnityEngine;
//using Direction = UnityEditor.VFX.Direction;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(DirectionType))]
    class VFXSlotDirection : VFXSlot
    {
        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Vector3>(Vector3.forward, mode);
        }

        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(Vector3);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, Type sourceSlotType)
        {
            if (sourceSlotType == typeof(VFXSlotDirection))
                return expression; //avoid multiple normalization

            return VFXOperatorUtility.Normalize(expression);
        }

        sealed protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return VFXOperatorUtility.Normalize(expr[0]);
        }

        sealed protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new[] { expr };
        }
    }
}
