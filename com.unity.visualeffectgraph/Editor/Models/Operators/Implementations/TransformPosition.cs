using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class TransformPosition : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the transform to be used in the transformation.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("Sets the position to be transformed.")]
            public Position position = Position.defaultValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the transformed position.")]
            public Vector3 tPos = Vector3.zero;
        }

        override public string name { get { return "Transform (Position)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformPosition(inputExpression[0], inputExpression[1]) };
        }
    }
}
