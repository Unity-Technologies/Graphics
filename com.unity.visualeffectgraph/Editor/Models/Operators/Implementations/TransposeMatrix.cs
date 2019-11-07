using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TransposeMatrix : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The Matrix4x4 to be transposed.")]
            public Matrix4x4 matrix = Matrix4x4.identity;
        }

        public class OutputProperties
        {
            public Matrix4x4 o = Matrix4x4.identity;
        }

        override public string name { get { return "Transpose (Matrix)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransposeMatrix(inputExpression[0]) };
        }
    }
}
