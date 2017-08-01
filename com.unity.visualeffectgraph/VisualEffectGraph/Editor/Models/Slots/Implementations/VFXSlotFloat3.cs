using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Vector3))]
    class VFXSlotFloat3 : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type)
                ||  type == typeof(float)
                ||  type == typeof(uint)
                ||  type == typeof(int)
                ||  type == typeof(Vector4)
                ||  type == typeof(Color);
        }

        sealed protected override bool CanConvertFrom(VFXExpression expr)
        {
            return base.CanConvertFrom(expr) || CanConvertFrom(VFXExpression.TypeToType(expr.ValueType));
        }

        sealed protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Vector3>(Vector3.zero, VFXValue.Mode.FoldableVariable);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.ValueType == VFXValueType.kFloat3)
                return expression;

            if (expression.ValueType == VFXValueType.kFloat)
                return new VFXExpressionCombine(expression, expression, expression);

            if (expression.ValueType == VFXValueType.kUint)
            {
                var floatExpression = new VFXExpressionCastUintToFloat(expression);
                return new VFXExpressionCombine(floatExpression, floatExpression, floatExpression);
            }

            if (expression.ValueType == VFXValueType.kInt)
            {
                var floatExpression = new VFXExpressionCastIntToFloat(expression);
                return new VFXExpressionCombine(floatExpression, floatExpression, floatExpression);
            }

            if (expression.ValueType == VFXValueType.kFloat4)
            {
                var x = new VFXExpressionExtractComponent(expression, 0);
                var y = new VFXExpressionExtractComponent(expression, 1);
                var z = new VFXExpressionExtractComponent(expression, 1);
                return new VFXExpressionCombine(x, y, z);
            }

            throw new Exception("Unexpected type of expression " + expression);
        }

        sealed protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1],
                expr[2]);
        }

        sealed protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[3]
            {
                new VFXExpressionExtractComponent(expr, 0),
                new VFXExpressionExtractComponent(expr, 1),
                new VFXExpressionExtractComponent(expr, 2)
            };
        }
    }
}
