using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(FlipBook))]
    class VFXSlotFlipBook : VFXSlot
    {
        sealed protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || (VFXSlotFloat2.CanConvertFromVector2(type) && type != typeof(Color));
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression, VFXSlot sourceSlot)
        {
            if (expression.valueType == VFXValueType.Float2)
            {
                return expression;
            }

            // Convert any expression to Vector2
            VFXExpression expressionX = null;
            VFXExpression expressionY = null;
            if (expression.valueType == VFXValueType.Int32)
            {
                expressionX = expressionY = new VFXExpressionCastIntToFloat(expression);
            }

            if (expression.valueType == VFXValueType.Uint32)
            {
                expressionX = expressionY = new VFXExpressionCastUintToFloat(expression);
            }

            if (expression.valueType == VFXValueType.Float)
            {
                expressionX = expressionY = expression;
            }

            if (expression.valueType == VFXValueType.Float3 || expression.valueType == VFXValueType.Float4)
            {
                expressionX = expression.x;
                expressionY = expression.y;
            }

            return new VFXExpressionCombine(expressionX, expressionY);
        }

        sealed public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<FlipBook>(FlipBook.defaultValue, mode);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(new VFXExpressionCastIntToFloat(expr[0]), new VFXExpressionCastIntToFloat(expr[1]));
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[2]
            {
                    new VFXExpressionCastFloatToInt(expr.x),
                    new VFXExpressionCastFloatToInt(expr.y)
            };
        }
    }
}
