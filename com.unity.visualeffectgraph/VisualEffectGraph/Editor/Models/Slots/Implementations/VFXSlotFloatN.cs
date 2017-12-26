using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(FloatN))]
    class VFXSlotFloatN : VFXSlot
    {
        protected override bool CanConvertFrom(VFXExpression expression)
        {
            return base.CanConvertFrom(expression) || VFXExpression.IsFloatValueType(expression.valueType);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return type == typeof(float)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Color)
                || type == typeof(uint)
                || type == typeof(int);
        }

        protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.valueType == VFXValueType.kUint)
            {
                var floatExpression = new VFXExpressionCastIntToFloat(expression);
                return floatExpression;
            }

            if (expression.valueType == VFXValueType.kInt)
            {
                var floatExpression = new VFXExpressionCastIntToFloat(expression);
                return floatExpression;
            }

            return expression;
        }

        protected override VFXValue DefaultExpression()
        {
            if (value == null)
                return null;

            var floatN = (FloatN)value;
            return floatN.ToVFXValue(VFXValue.Mode.FoldableVariable);
        }
    }
}
