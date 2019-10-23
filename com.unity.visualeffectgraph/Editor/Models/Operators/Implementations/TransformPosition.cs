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
            public Position position;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the transformed position.")]
            public Position pos;
        }

        override public string name { get { return "Transform (Position)"; } }

        public override void Sanitize(int version)
        {
            if (version < 4)
            {
                SanitizeHelper.MigrateVector3OutputToSpaceableKeepingLegacyBehavior(this, "Position");
            }
            base.Sanitize(version);
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformPosition(inputExpression[0], inputExpression[1]) };
        }
    }
}
