using System;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math", experimental = true)]
    class LengthNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            [Tooltip("The vector to be used in the length calculation.")]
            public Vector3 x;
        }

        public class OutputProperties
        {
            [Tooltip("The length of x.")]
            public float l;
        }

        public sealed override string name
        {
            get
            {
                return "LengthNew";
            }
        }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowVectorType; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }
}
