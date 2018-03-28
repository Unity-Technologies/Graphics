using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Geometry")]
    class TransformPosition : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The transform.")]
            public Transform transform = new Transform();
            [Tooltip("The position to be transformed.")]
            public Position position = new Position();
        }

        public class OutputProperties
        {
            public Vector3 tPos;
        }

        override public string name { get { return "Transform (Position)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformPosition(inputExpression[0], inputExpression[1]) };
        }
    }
}
