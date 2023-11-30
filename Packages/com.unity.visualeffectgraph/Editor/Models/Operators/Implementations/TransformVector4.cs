using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Transform(Vector4)")]
    [VFXInfo(name = "Transform (Vector4)", category = "Math/Geometry")]
    class TransformVector4 : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the transform to be used in the transformation.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("Sets the Vector4 to be transformed.")]
            public Vector4 vec = Vector4.zero;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the transformed Vector4.")]
            public Vector4 tVec;
        }

        override public string name { get { return "Transform (Vector4)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformVector4(inputExpression[0], inputExpression[1]) };
        }
    }
}
