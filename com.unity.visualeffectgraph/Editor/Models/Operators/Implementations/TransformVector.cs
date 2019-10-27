using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TransformVector : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the transform to be used in the transformation.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("Sets the vector to be transformed.")]
            public Vector vector = Vector.defaultValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the transformed vector.")]
            public Vector3 tVec = Vector3.zero;
        }

        override public string name { get { return "Transform (Vector)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformVector(inputExpression[0], inputExpression[1]) };
        }
    }
}
