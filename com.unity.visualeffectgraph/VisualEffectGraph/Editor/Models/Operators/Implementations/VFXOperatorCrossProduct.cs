using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Vector")]
	class VFXOperatorCrossProduct : VFXOperator
    {
        public class InputProperties
        {
			[Tooltip("The first operand.")]
			public Vector3 a = Vector3.right;
			[Tooltip("The second operand.")]
			public Vector3 b = Vector3.up;
        }

        override public string name { get { return "Cross Product"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var lhs = VFXOperatorUtility.ExtractComponents(inputExpression[0]).ToArray();
            var rhs = VFXOperatorUtility.ExtractComponents(inputExpression[1]).ToArray();

            Func<VFXExpression, VFXExpression, VFXExpression, VFXExpression, VFXExpression> ab_Minus_cd = delegate(VFXExpression a, VFXExpression b, VFXExpression c, VFXExpression d)
                {
                    var ab = new VFXExpressionMul(a, b);
                    var cd = new VFXExpressionMul(c, d);
                    return new VFXExpressionSubtract(ab, cd);
                };

            return new[] { new VFXExpressionCombine(new[]
                {
                    ab_Minus_cd(lhs[1], rhs[2], lhs[2], rhs[1]),
                    ab_Minus_cd(lhs[2], rhs[0], lhs[0], rhs[2]),
                    ab_Minus_cd(lhs[0], rhs[1], lhs[1], rhs[0]),
                })};
        }
    }
}
