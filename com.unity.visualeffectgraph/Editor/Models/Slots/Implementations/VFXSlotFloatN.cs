using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(FloatN))]
    class VFXSlotFloatN : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return type == typeof(float)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Color)
                || type == typeof(uint)
                || type == typeof(int);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            if (expression.valueType == VFXValueType.Uint32)
            {
                var floatExpression = new VFXExpressionCastUintToFloat(expression);
                return floatExpression;
            }

            if (expression.valueType == VFXValueType.Int32)
            {
                var floatExpression = new VFXExpressionCastIntToFloat(expression);
                return floatExpression;
            }

            return expression;
        }

        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            if (value == null)
                return null;

            var floatN = (FloatN)value;
            return floatN.ToVFXValue(mode);
        }
    }
}
