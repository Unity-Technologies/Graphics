using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Coordinates")]
    class SphericalToRectangular : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The radial coordinate (Radius).")]
            public float distance = 1.0f;
            [Tooltip("The angular coordinate (Polar angle) in radians.")]
            public float theta = 45.0f;
            [Tooltip("The pitch coordinate (Azimuth angle) in radians.")]
            public float phi = 45.0f;
        }

        public class OutputProperties
        {
            public Vector3 coord = Vector3.zero;
        }

        override public string name { get { return "Spherical to Rectangular"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.SphericalToRectangular(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
