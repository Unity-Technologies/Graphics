using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
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
            public DirectionType dir;
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
