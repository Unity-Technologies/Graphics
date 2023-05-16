using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-InvertTRS(Matrix)")]
    [VFXInfo(category = "Math/Geometry")]
    class TransformMatrix : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the transform to be used in the transformation.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("Sets the Matrix4x4 to be transformed.")]
            public Matrix4x4 matrix = Matrix4x4.identity;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the transformed Matrix4x4.")]
            public Matrix4x4 o = Matrix4x4.identity;
        }

        override public string name { get { return "Transform (Matrix)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformMatrix(inputExpression[0], inputExpression[1]) };
        }
    }
}
