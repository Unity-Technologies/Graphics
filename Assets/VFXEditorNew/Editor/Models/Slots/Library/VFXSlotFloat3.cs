using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Vector3))]
    class VFXSlotFloat3 : VFXSlot
    {
        protected override VFXValue DefaultExpression()
        {
            return new VFXValueFloat3(Vector3.zero, false);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionCombine(
                expr[0],
                expr[1],
                expr[2]);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
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
