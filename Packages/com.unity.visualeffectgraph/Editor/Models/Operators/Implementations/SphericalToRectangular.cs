using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SphericalToRectangular")]
    [VFXInfo(category = "Math/Coordinates")]
    class SphericalToRectangular : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the radial coordinate (Radius r).")]
            public float distance = 1.0f;
            [Tooltip("Sets the angular coordinate (Polar angle θ) in radians.")]
            public float theta = 45.0f;
            [Tooltip("Sets the pitch coordinate (Azimuth angle ϕ) in radians.")]
            public float phi = 45.0f;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the spherical coordinates (r,θ,ϕ) in rectangular (x,y,z) coordinates.")]
            public Vector3 coord = Vector3.zero;
        }

        override public string name { get { return "Spherical to Rectangular"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.SphericalToRectangular(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
