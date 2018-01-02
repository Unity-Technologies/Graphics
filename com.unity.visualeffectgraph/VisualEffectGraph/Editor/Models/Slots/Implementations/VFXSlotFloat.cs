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

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.valueType == VFXValueType.kFloat)
                return expression;

            if (expression.valueType == VFXValueType.kInt)
                return new VFXExpressionCastIntToFloat(expression);

            if (expression.valueType == VFXValueType.kUint)
                return new VFXExpressionCastUintToFloat(expression);

            if (expression.valueType == VFXValueType.kFloat2
                ||  expression.valueType == VFXValueType.kFloat3
                ||  expression.valueType == VFXValueType.kFloat4)
            {
                return expression.x;
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<float>(0.0f, VFXValue.Mode.FoldableVariable);
        }
    }
}
