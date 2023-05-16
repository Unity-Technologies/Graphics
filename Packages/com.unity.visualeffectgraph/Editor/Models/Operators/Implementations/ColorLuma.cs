using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-ColorLuma")]
    [VFXInfo(category = "Color")]
    class ColorLuma : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the color used for the luminance calculation.")]
            public Color color = Color.white;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the luminance (perceived brightness) of the color.")]
            public float luma;
        }

        override public string name { get { return "Color Luma"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.ColorLuma(inputExpression[0]) };
        }
    }
}
