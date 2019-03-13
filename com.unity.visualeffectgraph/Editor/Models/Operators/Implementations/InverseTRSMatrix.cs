using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class InverseTRSMatrix : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The Matrix4x4 to be inverted (should not be singular).")]
            public Matrix4x4 matrix = Matrix4x4.identity;
        }

        public class OutputProperties
        {
            public Matrix4x4 o = Matrix4x4.identity;
        }

        override public string name { get { return "InvertTRS (Matrix)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionInverseTRSMatrix(inputExpression[0]) };
        }
    }
}
