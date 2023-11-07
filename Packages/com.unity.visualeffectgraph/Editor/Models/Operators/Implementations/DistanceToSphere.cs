using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Distance(Sphere)")]
    [VFXInfo(name = "Distance (Sphere)", category = "Math/Geometry")]
    class DistanceToSphere : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the sphere used for the distance calculation.")]
            public Sphere sphere = Sphere.defaultValue;
            [Tooltip("Sets the position used for the distance calculation.")]
            public Position position = new Position();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the closest point on the sphere to the supplied position.")]
            public Position closestPosition;
            [Tooltip("Outputs the signed distance from the sphere. Negative values represent points that are inside the sphere.")]
            public float distance;
        }

        override public string name { get { return "Distance (Sphere)"; } }

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
            VFXExpression sphereDelta = (inputExpression[2] - inputExpression[0]);
            VFXExpression sphereDeltaLength = VFXOperatorUtility.Length(sphereDelta);
            VFXExpression sphereDistance = (sphereDeltaLength - inputExpression[1]);

            VFXExpression pointOnSphere = (inputExpression[1] / sphereDeltaLength);
            pointOnSphere = (sphereDelta * VFXOperatorUtility.CastFloat(pointOnSphere, inputExpression[0].valueType) + inputExpression[0]);

            return new VFXExpression[] { pointOnSphere, sphereDistance };
        }
    }
}
