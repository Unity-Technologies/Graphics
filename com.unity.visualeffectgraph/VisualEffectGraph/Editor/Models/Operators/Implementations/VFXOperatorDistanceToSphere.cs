using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorDistanceToSphere : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The sphere used for the distance calculation.")]
            public Sphere sphere = new Sphere();
            [Tooltip("The position used for the distance calculation.")]
            public Position position = new Position();
        }

        public class OutputProperties
        {
            [Tooltip("The closest point on the sphere to the supplied position.")]
            public Vector3 closestPosition;
            [Tooltip("The signed distance from the sphere. (Negative values represent points that are inside the sphere).")]
            public float distance;
        }

        override public string name { get { return "Distance (Sphere)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression sphereDelta = new VFXExpressionSubtract(inputExpression[2], inputExpression[0]);
            VFXExpression sphereDeltaLength = VFXOperatorUtility.Length(sphereDelta);
            VFXExpression sphereDistance = new VFXExpressionSubtract(sphereDeltaLength, inputExpression[1]);

            VFXExpression pointOnSphere = new VFXExpressionDivide(inputExpression[1], sphereDeltaLength);
            pointOnSphere = new VFXExpressionAdd(new VFXExpressionMul(sphereDelta, VFXOperatorUtility.CastFloat(pointOnSphere, inputExpression[0].valueType)), inputExpression[0]);

            return new VFXExpression[] { pointOnSphere, sphereDistance };
        }
    }
}
