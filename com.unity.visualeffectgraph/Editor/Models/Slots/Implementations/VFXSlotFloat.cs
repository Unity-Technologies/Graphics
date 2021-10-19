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
                || type == typeof(bool)
                || type == typeof(uint)
                || type == typeof(int)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Color);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            switch (expression.valueType)
            {
                case VFXValueType.Float:
                    return expression;
                case VFXValueType.Boolean:
                    return new VFXExpressionCastBoolToFloat(expression);
                case VFXValueType.Int32:
                    return new VFXExpressionCastIntToFloat(expression);
                case VFXValueType.Uint32:
                    return new VFXExpressionCastUintToFloat(expression);
                case VFXValueType.Float2:
                case VFXValueType.Float3:
                case VFXValueType.Float4:
                    return expression.x;
                default:
                    throw new Exception("Unexpected type of expression " + expression);
            }
        }

        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<float>(0.0f, mode);
        }
    }
}
