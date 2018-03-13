using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;
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
            return base.CanConvertFrom(type) || type == typeof(Vector4) || type == typeof(Vector3);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            if (sourceSlot != null)
            {
                if (sourceSlot.GetType() == typeof(VFXSlotDirection))
                    return expression; //avoid multiple normalization
                if (sourceSlot.property.attributes != null && sourceSlot.property.attributes.OfType<NormalizeAttribute>().Any())
                    return expression; //avoid multiple normalization form Normalize attribute (rarely used for output slot)
            }

            if (expression.valueType == VFXValueType.Float4)
                expression = VFXOperatorUtility.CastFloat(expression, VFXValueType.Float3);

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
