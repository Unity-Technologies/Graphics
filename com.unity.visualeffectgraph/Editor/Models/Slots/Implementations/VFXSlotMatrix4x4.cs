using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Matrix4x4))]
    class VFXSlotMatrix4x4 : VFXSlot
    {
        protected override bool CanConvertFrom(Type type)
        {
            return base.CanConvertFrom(type) || type == typeof(Transform);
        }

        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXValue<Matrix4x4>(Matrix4x4.identity, mode);
        }

        protected override VFXExpression ExpressionFromChildren(VFXExpression[] expr)
        {
            return new VFXExpressionVector4sToMatrix(expr);
        }

        protected override VFXExpression[] ExpressionToChildren(VFXExpression expr)
        {
            return new VFXExpression[]
            {
                new VFXExpressionMatrixToVector4s(expr, VFXValue.Constant<int>(0)),
                new VFXExpressionMatrixToVector4s(expr, VFXValue.Constant<int>(1)),
                new VFXExpressionMatrixToVector4s(expr, VFXValue.Constant<int>(2)),
                new VFXExpressionMatrixToVector4s(expr, VFXValue.Constant<int>(3))
            };
        }
    }
}
