using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Color")]
    class RGBtoHSV : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the color to be converted to Hue, Saturation, and Value parameters.")]
            public Color color = Color.white;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the Hue, Saturation, and Value parameters derived from the input color.")]
            public Vector3 HSV = Vector3.zero;
        }

        override public string name { get { return "RGB to HSV"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var components = VFXOperatorUtility.ExtractComponents(inputExpression[0]);
            VFXExpression rgb = new VFXExpressionCombine(components.Take(3).ToArray());

            return new[] { new VFXExpressionRGBtoHSV(rgb) };
        }
    }
}
