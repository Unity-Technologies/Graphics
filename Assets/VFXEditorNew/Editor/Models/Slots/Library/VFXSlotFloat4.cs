using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Vector4))]
    class VFXSlotFloat4 : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValueFloat4(Vector4.zero, false);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1],
                expr[2],
                expr[3]);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[4]
            {
                new VFXExpressionExtractComponent(expr, 0),
                new VFXExpressionExtractComponent(expr, 1),
                new VFXExpressionExtractComponent(expr, 2),
                new VFXExpressionExtractComponent(expr, 3)
            };
        }
    }
}
