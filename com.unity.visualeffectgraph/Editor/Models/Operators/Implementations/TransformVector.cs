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
            public Vector tVec;
        }

        override public string name { get { return "Transform (Vector)"; } }

        public override void Sanitize(int version)
        {
            if (version < 4)
            {
                SanitizeHelper.MigrateVector3OutputToSpaceableKeepingLegacyBehavior(this, "Vector");
            }
            base.Sanitize(version);
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformVector(inputExpression[0], inputExpression[1]) };
        }
    }
}
