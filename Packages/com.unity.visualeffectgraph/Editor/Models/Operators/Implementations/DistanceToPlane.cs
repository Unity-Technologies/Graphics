using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Distance(Plane)")]
    [VFXInfo(name = "Distance (Plane)", category = "Math/Geometry")]
    class DistanceToPlane : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the plane used for the distance calculation.")]
            public Plane plane = new Plane();
            [Tooltip("Sets the position used for the distance calculation.")]
            public Position position = new Position();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the closest point on the plane to the supplied position.")]
            public Position closestPosition;
            [Tooltip("Outputs the signed distance from the plane.")]
            public float distance;
        }

        public override void Sanitize(int version)
        {
            if (version < 4)
            {
                SanitizeHelper.MigrateVector3OutputToSpaceableKeepingLegacyBehavior(this, "Position");
            }
            base.Sanitize(version);
        }

        override public string name { get { return "Distance (Plane)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression planeDistance = VFXOperatorUtility.SignedDistanceToPlane(inputExpression[0], inputExpression[1], inputExpression[2]);
            VFXExpression pointOnPlane = (inputExpression[2] - inputExpression[1] * VFXOperatorUtility.CastFloat(planeDistance, inputExpression[1].valueType));
            return new VFXExpression[] { pointOnPlane, planeDistance };
        }
    }
}
