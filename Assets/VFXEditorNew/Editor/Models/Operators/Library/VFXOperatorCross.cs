using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorCross : VFXOperator
    {
        public class InputProperties
        {
            public Vector3 left = Vector3.right;
            public Vector3 right = Vector3.up;
        }

        override public string name { get { return "Cross"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var lhs = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();
            var rhs = VFXOperatorUtility.ExtractComponents(inputExpression[1]).ToArray();

            Func<VFXExpression, VFXExpression, VFXExpression, VFXExpression, VFXExpression> ab_Minus_cd = delegate (VFXExpression a, VFXExpression b, VFXExpression c, VFXExpression d)
            {
                var ab = new VFXExpressionMul(a, b);
                var cd = new VFXExpressionMul(c, d);
                return new VFXExpressionSubtract(ab, cd);
            };

            return new[] { new VFXExpressionCombine(new []
            {
                ab_Minus_cd(lhs[1], rhs[2], lhs[2], rhs[1]),
                ab_Minus_cd(lhs[2], rhs[0], lhs[0], rhs[2]),
                ab_Minus_cd(lhs[0], rhs[1], lhs[1], rhs[0]),
            })};
        }
    }
}