using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(int))]
    class VFXSlotInt : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || type == typeof(uint)
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
            if (expression.ValueType == VFXValueType.kInt)
            {
                return expression;
            }

            if (expression.ValueType == VFXValueType.kUint)
            {
                return new VFXExpressionCastUintToInt(expression);
            }

            if (expression.ValueType == VFXValueType.kFloat)
            {
                return new VFXExpressionCastFloatToInt(expression);
            }

            if (expression.ValueType == VFXValueType.kFloat2
                ||  expression.ValueType == VFXValueType.kFloat3
                ||  expression.ValueType == VFXValueType.kFloat4)
            {
                var floatExpression = new VFXExpressionExtractComponent(expression, 0);
                return new VFXExpressionCastFloatToInt(expression);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<int>(0, VFXValue.Mode.FoldableVariable);
        }
    }
}
