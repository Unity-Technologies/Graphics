using System;
using UnityEngine;

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
            return base.CanConvertFrom(expr) || CanConvertFrom(VFXExpression.TypeToType(expr.ValueType));
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.ValueType == VFXValueType.kUint)
            {
                return expression;
            }

            if (expression.ValueType == VFXValueType.kInt)
            {
                return new VFXExpressionCastIntToUint(expression);
            }

            if (expression.ValueType == VFXValueType.kFloat)
            {
                return new VFXExpressionCastFloatToUint(expression);
            }

            if (expression.ValueType == VFXValueType.kFloat2
                ||  expression.ValueType == VFXValueType.kFloat3
                ||  expression.ValueType == VFXValueType.kFloat4)
            {
                var floatExpression = new VFXExpressionExtractComponent(expression, 0);
                return new VFXExpressionCastFloatToUint(expression);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<uint>(0u, VFXValue.Mode.FoldableVariable);
        }
    }
}
