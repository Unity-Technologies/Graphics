using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-RectangularToPolar")]
    [VFXInfo(category = "Math/Coordinates")]
    class RectangularToPolar : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the rectangular (x,y) coordinates to convert to polar coordinates (r,θ).")]
            public Vector2 coordinate = Vector2.zero;
        }
        public class OutputProperties
        {
            [Angle, Tooltip("Outputs the angular coordinate (Polar angle).")]
            public float theta = Mathf.PI / 2;
            [Tooltip("Outputs the radial coordinates (Radius).")]
            public float distance = 1.0f;
        }

        override public string name { get { return "Rectangular to Polar"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return VFXOperatorUtility.RectangularToPolar(inputExpression[0]);
        }
    }
}
