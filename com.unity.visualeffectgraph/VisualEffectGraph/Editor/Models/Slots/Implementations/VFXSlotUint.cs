using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(uint))]
    class VFXSlotUint : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || type == typeof(int)
                || type == typeof(float)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Color);
        }

        sealed protected override bool CanConvertFrom(VFXExpression expr)
        {
            return base.CanConvertFrom(expr) || CanConvertFrom(VFXExpression.TypeToType(expr.valueType));
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.valueType == VFXValueType.kUint)
            {
                return expression;
            }

            if (expression.valueType == VFXValueType.kInt)
            {
                return new VFXExpressionCastIntToUint(expression);
            }

            if (expression.valueType == VFXValueType.kFloat)
            {
                return new VFXExpressionCastFloatToUint(expression);
            }

            if (expression.valueType == VFXValueType.kFloat2
                ||  expression.valueType == VFXValueType.kFloat3
                ||  expression.valueType == VFXValueType.kFloat4)
            {
                return new VFXExpressionCastFloatToUint(expression.x);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<uint>(0u, VFXValue.Mode.FoldableVariable);
        }
    }
}
