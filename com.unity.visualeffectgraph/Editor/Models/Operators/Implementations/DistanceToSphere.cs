using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Geometry")]
    class DistanceToSphere : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the sphere used for the distance calculation.")]
            public TSphere sphere = new TSphere();
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

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var sphereTransform = inputExpression[0];
            var sphereRadius = inputExpression[1];
            var initialPosition = inputExpression[2];

            var finalTransfom = new VFXExpressionTransformMatrix(sphereTransform, VFXOperatorUtility.UniformScaleMatrix(sphereRadius));
            var invFinalTransform = new VFXExpressionInverseMatrix(finalTransfom);

            var transformedPosition = new VFXExpressionTransformPosition(invFinalTransform, initialPosition);

            var sphereDeltaLength = VFXOperatorUtility.Length(transformedPosition);
            var sign = new VFXExpressionSign(VFXOperatorUtility.OneExpression[VFXValueType.Float] - sphereDeltaLength);

            var pointOnSphere = transformedPosition / VFXOperatorUtility.CastFloat(sphereDeltaLength, VFXValueType.Float3);
            var finalPos = new VFXExpressionTransformPosition(finalTransfom, pointOnSphere);

            var distance = VFXOperatorUtility.Length(finalPos - initialPosition) * sign;
            return new VFXExpression[] { finalPos, distance };
        }
    }
}
