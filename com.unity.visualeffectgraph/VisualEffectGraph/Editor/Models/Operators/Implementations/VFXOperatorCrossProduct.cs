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

        public class OutputProperties
        {
            public Vector3 o;
        }

        override public string name { get { return "Cross Product"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Cross(inputExpression[0], inputExpression[1]) };
        }
    }
}
