using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Vector2))]
    class VFXSlotFloat2 : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                ||  type == typeof(float)
                ||  type == typeof(uint)
                ||  type == typeof(int)
                ||  type == typeof(Vector3)
                ||  type == typeof(Vector4)
                ||  type == typeof(Color);
        }

        sealed protected override bool CanConvertFrom(VFXExpression expr)
        {
            return base.CanConvertFrom(expr) || CanConvertFrom(VFXExpression.TypeToType(expr.ValueType));
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Vector2>(Vector2.zero, VFXValue.Mode.FoldableVariable);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.ValueType == VFXValueType.kFloat2)
                return expression;

            if (expression.ValueType == VFXValueType.kFloat)
                return new VFXExpressionCombine(expression, expression);

            if (expression.ValueType == VFXValueType.kUint)
            {
                var floatExpression = new VFXExpressionCastUintToFloat(expression);
                return new VFXExpressionCombine(floatExpression, floatExpression);
            }

            if (expression.ValueType == VFXValueType.kInt)
            {
                var floatExpression = new VFXExpressionCastIntToFloat(expression);
                return new VFXExpressionCombine(floatExpression, floatExpression);
            }

            if (expression.ValueType == VFXValueType.kFloat3 || expression.ValueType == VFXValueType.kFloat4)
            {
                var x = new VFXExpressionExtractComponent(expression, 0);
                var y = new VFXExpressionExtractComponent(expression, 1);
                return new VFXExpressionCombine(x, y);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1]);
        }

        sealed protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[2]
            {
                new VFXExpressionExtractComponent(expr, 0),
                new VFXExpressionExtractComponent(expr, 1)
            };
        }
    }
}
