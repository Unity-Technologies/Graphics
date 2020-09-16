using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class DistanceToSphere : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the sphere used for the distance calculation.")]
            public Sphere sphere = new Sphere();
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
            var spherePosition = inputExpression[0];
            var sphereRadius = inputExpression[2];
            var inputPosition = inputExpression[3];

            VFXExpression sphereDelta = inputPosition - spherePosition;
            VFXExpression sphereDeltaLength = VFXOperatorUtility.Length(sphereDelta);
            VFXExpression sphereDistance = sphereDeltaLength - sphereRadius;

            VFXExpression pointOnSphere = sphereRadius / sphereDeltaLength;
            pointOnSphere = (sphereDelta * VFXOperatorUtility.CastFloat(pointOnSphere, spherePosition.valueType) + spherePosition);

            return new VFXExpression[] { pointOnSphere, sphereDistance };
        }
    }
}
