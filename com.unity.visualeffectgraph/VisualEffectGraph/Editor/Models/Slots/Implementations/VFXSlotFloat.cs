using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(float))]
    class VFXSlotFloat : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                ||  type == typeof(uint)
                ||  type == typeof(int)
                ||  type == typeof(Vector2)
                ||  type == typeof(Vector3)
                ||  type == typeof(Vector4)
                ||  type == typeof(Color);
        }

        sealed protected override bool CanConvertFrom(VFXExpression expr)
        {
            return base.CanConvertFrom(expr) || CanConvertFrom(VFXExpression.TypeToType(expr.ValueType));
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.ValueType == VFXValueType.kFloat)
                return expression;

            if (expression.ValueType == VFXValueType.kInt)
                return new VFXExpressionCastIntToFloat(expression);

            if (expression.ValueType == VFXValueType.kUint)
                return new VFXExpressionCastUintToFloat(expression);

            if (expression.ValueType == VFXValueType.kFloat2
                ||  expression.ValueType == VFXValueType.kFloat3
                ||  expression.ValueType == VFXValueType.kFloat4)
            {
                return new VFXExpressionExtractComponent(expression, 0);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<float>(0.0f, VFXValue.Mode.FoldableVariable);
        }
    }
}
