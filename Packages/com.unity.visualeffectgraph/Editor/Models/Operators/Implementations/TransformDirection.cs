using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Transform(Direction)")]
    [VFXInfo(name = "Transform (Direction)", category = "Math/Geometry")]
    class TransformDirection : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the transform to be used in the transformation.")]
            public Transform transform = Transform.defaultValue;
            [Tooltip("Sets the normalized vector to be transformed.")]
            public DirectionType direction = DirectionType.defaultValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the transformed normalized vector.")]
            public DirectionType tDir;
        }

        public override void Sanitize(int version)
        {
            if (version < 4)
            {
                SanitizeHelper.MigrateVector3OutputToSpaceableKeepingLegacyBehavior(this, "DirectionType");
            }
            base.Sanitize(version);
        }

        override public string name { get { return "Transform (Direction)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { new VFXExpressionTransformDirection(inputExpression[0], inputExpression[1]) };
        }
    }
}
