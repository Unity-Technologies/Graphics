using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-RectangularToSpherical")]
    [VFXInfo(category = "Math/Coordinates")]
    class RectangularToSpherical : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the rectangular (x,y,z) coordinates to convert to spherical coordinates (r,θ,ϕ).")]
            public Vector3 coordinate = Vector3.zero;
        }
        public class OutputProperties
        {
            [Tooltip("Outputs the radial coordinate (Radius r).")]
            public float distance = 1.0f;
            [Angle, Tooltip("Outputs the angular coordinate (Polar angle θ) in radians.")]
            public float theta = Mathf.PI / 2;
            [Angle, Tooltip("Outputs the pitch coordinate (Azimuth angle ϕ) in radians.")]
            public float phi = Mathf.PI / 2;
        }
        override public string name { get { return "Rectangular to Spherical"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return VFXOperatorUtility.RectangularToSpherical(inputExpression[0]);
        }
    }
}
