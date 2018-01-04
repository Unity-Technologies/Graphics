using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Transform))]
    class VFXSlotTransform : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValue<Matrix4x4>(Matrix4x4.identity, VFXValue.Mode.FoldableVariable);
        }

        protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(Matrix4x4);
        }

        sealed protected override VFXExpression ConvertExpression(VFXExpression expression)
        {
            if (expression.valueType == VFXValueType.kTransform)
                return expression;

            throw new Exception("Unexpected type of expression " + expression);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionTRSToMatrix(expr);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[3]
            {
                new VFXExpressionExtractPositionFromMatrix(expr),
                new VFXExpressionExtractAnglesFromMatrix(expr),
                new VFXExpressionExtractScaleFromMatrix(expr)
            };
        }
    }
}
