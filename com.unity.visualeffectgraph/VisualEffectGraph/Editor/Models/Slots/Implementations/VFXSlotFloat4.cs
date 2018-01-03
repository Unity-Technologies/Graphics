using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Vector4))]
    class VFXSlotFloat4 : VFXSlot
    {
        protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                || type == typeof(float)
                || type == typeof(uint)
                || type == typeof(int)
                || type == typeof(Color);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.valueType == VFXValueType.kFloat4)
                return expression;

            if (expression.valueType == VFXValueType.kFloat)
                return new VFXExpressionCombine(expression, expression, expression, expression);

            if (expression.valueType == VFXValueType.kUint)
            {
                var floatExpression = new VFXExpressionCastUintToFloat(expression);
                return new VFXExpressionCombine(floatExpression, floatExpression, floatExpression, floatExpression);
            }

            if (expression.valueType == VFXValueType.kInt)
            {
                var floatExpression = new VFXExpressionCastIntToFloat(expression);
                return new VFXExpressionCombine(floatExpression, floatExpression, floatExpression, floatExpression);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Vector4>(Vector4.zero, VFXValue.Mode.FoldableVariable);
        }

        sealed protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1],
                expr[2],
                expr[3]);
        }

        sealed protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[4]
            {
                expr.x,
                expr.y,
                expr.z,
                expr.w
            };
        }
    }
}
