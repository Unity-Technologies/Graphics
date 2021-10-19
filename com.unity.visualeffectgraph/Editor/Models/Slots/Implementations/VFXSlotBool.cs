using System;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(bool))]
    class VFXSlotBool : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || type == typeof(uint)
                || type == typeof(int)
                || type == typeof(float);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            switch (expression.valueType)
            {
                case VFXValueType.Boolean:
                    return expression;
                case VFXValueType.Uint32:
                    return new VFXExpressionCastUintToBool(expression);
                case VFXValueType.Int32:
                    return new VFXExpressionCastIntToBool(expression);
                case VFXValueType.Float:
                    return new VFXExpressionCastFloatToBool(expression);
                default:
                    throw new Exception("Unexpected type of expression " + expression);
            }
        }

        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<bool>(false, mode);
        }
    }
}
