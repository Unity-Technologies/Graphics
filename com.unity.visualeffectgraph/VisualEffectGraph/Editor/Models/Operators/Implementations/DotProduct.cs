using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class DotProduct : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            [Tooltip("The first operand.")]
            public FloatN a = Vector3.zero;
            [Tooltip("The second operand.")]
            public FloatN b = Vector3.zero;
        }

        public class OutputProperties
        {
            [Tooltip("The dot product between a and b.")]
            public float d;
        }

        override public string name { get { return "Dot Product"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Dot(inputExpression[0], inputExpression[1]) };
        }
    }
}
