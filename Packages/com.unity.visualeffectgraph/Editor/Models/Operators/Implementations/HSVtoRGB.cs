using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-HSVToRGB")]
    [VFXInfo(name = "HSV to RGB", category = "Color", synonyms = new []{ "Hue", "Saturation", "Value", "Convert" })]
    class HSVtoRGB : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the Hue, Saturation, and Value parameters to be converted to color values.")]
            public Vector3 HSV = new Vector3(1.0f, 0.5f, 0.5f);
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the color values derived from the Hue, Saturation, and Value parameters.")]
            public Vector4 RGB = Vector4.zero;
        }

        override public string name { get { return "HSV to RGB"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] rgb = VFXOperatorUtility.ExtractComponents(new VFXExpressionHSVtoRGB(inputExpression[0])).Take(3).ToArray();
            return new[] { new VFXExpressionCombine(new[] { rgb[0], rgb[1], rgb[2], VFXValue.Constant(1.0f) }) };
        }
    }
}
