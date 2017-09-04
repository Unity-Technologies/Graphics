using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorDistanceToPlane : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The plane used for the distance calculation.")]
            public Plane plane = new Plane();
            [Tooltip("The position used for the distance calculation.")]
            public Position position = new Position();
        }

        override public string name { get { return "Distance (Plane)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression planeDistance = VFXOperatorUtility.SignedDistanceToPlane(inputExpression[0], inputExpression[1], inputExpression[2]);
            VFXExpression pointOnPlane = new VFXExpressionSubtract(inputExpression[2], new VFXExpressionMul(inputExpression[1], VFXOperatorUtility.CastFloat(planeDistance, inputExpression[1].ValueType)));
            return new VFXExpression[] { pointOnPlane, planeDistance };
        }
    }
}
