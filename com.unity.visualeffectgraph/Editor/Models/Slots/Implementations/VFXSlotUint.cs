using System;
using UnityEngine;
using UnityEngine.VFX;

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

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            if (expression.valueType == VFXValueType.Uint32)
            {
                return expression;
            }

            if (expression.valueType == VFXValueType.Int32)
            {
                return new VFXExpressionCastIntToUint(expression);
            }

            if (expression.valueType == VFXValueType.Float)
            {
                return new VFXExpressionCastFloatToUint(expression);
            }

            if (expression.valueType == VFXValueType.Float2
                || expression.valueType == VFXValueType.Float3
                || expression.valueType == VFXValueType.Float4)
            {
                return new VFXExpressionCastFloatToUint(expression.x);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<uint>(0u, mode);
        }
    }
}
