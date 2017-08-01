using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(FloatN))]
    class VFXSlotFloatN : VFXSlot
    {
        protected override bool CanConvertFrom(VFXExpression expression)
        {
            return base.CanConvertFrom(expression) || VFXExpression.IsFloatValueType(expression.ValueType);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return type == typeof(float)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Color);
        }

        protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
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
