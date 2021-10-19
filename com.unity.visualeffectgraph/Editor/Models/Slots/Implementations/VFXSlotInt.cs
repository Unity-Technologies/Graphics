using System;
using UnityEngine;
using UnityEngine.VFX;

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
                || type == typeof(bool)
                || type == typeof(Vector2)
                || type == typeof(Vector3)
                || type == typeof(Vector4)
                || type == typeof(Color);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            switch (expression.valueType)
            {
                case VFXValueType.Int32:
                    return expression;
                case VFXValueType.Uint32:
                    return new VFXExpressionCastUintToInt(expression);
                case VFXValueType.Float:
                    return new VFXExpressionCastFloatToInt(expression);
                case VFXValueType.Boolean:
                    return new VFXExpressionCastBoolToInt(expression);
                case VFXValueType.Float2:
                case VFXValueType.Float3:
                case VFXValueType.Float4:
                    return new VFXExpressionCastFloatToInt(expression.x);
                default:
                    throw new Exception("Unexpected type of expression " + expression);
            }
        }

        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<int>(0, mode);
        }
    }
}
