using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-PolarToRectangular")]
    [VFXInfo(category = "Math/Coordinates")]
    class PolarToRectangular : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the angular coordinate (Polar angle).")]
            public float Angle = 45.0f;
            [Tooltip("Sets the radial coordinate (Radius).")]
            public float distance = 1.0f;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the polar coordinates (r,θ) in rectangular (x,y) coordinates.")]
            public Vector2 coord = Vector2.zero;
        }

        override public string name { get { return "Polar to Rectangular"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.PolarToRectangular(VFXOperatorUtility.DegToRad(inputExpression[0]), inputExpression[1]) };
        }
    }
}
