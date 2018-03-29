using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Geometry")]
    class TransformDirection : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The transform.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("The normalized vector to be transformed.")]
            public DirectionType direction = DirectionType.defaultValue;
        }

        public class OutputProperties
        {
            public Vector3 tDir;
        }

        override public string name { get { return "Transform (Direction)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformDirection(inputExpression[0], inputExpression[1]) };
        }
    }
}
