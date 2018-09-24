using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector")]
    class CrossProduct : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The first operand.")]
            public Vector3 a = Vector3.right;
            [Tooltip("The second operand.")]
            public Vector3 b = Vector3.up;
        }

        public class OutputProperties
        {
            public Vector3 o = Vector3.zero;
        }

        override public string name { get { return "Cross Product"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Cross(inputExpression[0], inputExpression[1]) };
        }
    }
}
