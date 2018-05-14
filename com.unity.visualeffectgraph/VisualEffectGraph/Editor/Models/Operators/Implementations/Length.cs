using System;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
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
                return "Length";
            }
        }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }
}
