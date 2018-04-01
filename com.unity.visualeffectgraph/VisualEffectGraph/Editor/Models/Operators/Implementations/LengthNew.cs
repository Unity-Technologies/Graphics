using System;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class LengthNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            [Tooltip("The vector to be used in the length calculation.")]
            public Vector3 x = Vector3.one;
        }

        public class OutputProperties
        {
            [Tooltip("The length of x.")]
            public float l;
        }

        protected override sealed bool allowInteger { get { return false; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }
}
