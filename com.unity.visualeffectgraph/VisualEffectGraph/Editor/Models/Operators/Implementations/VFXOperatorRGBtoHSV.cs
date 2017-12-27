using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Color")]
    class VFXOperatorRGBtoHSV : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The color to be converted to HSV.")]
            public Color color = Color.white;
        }

        public class OutputProperties
        {
            public Vector3 hsv;
        }

        override public string name { get { return "RGB to HSV"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var components = VFXOperatorUtility.ExtractComponents(inputExpression[0]);
            VFXExpression rgb = new VFXExpressionCombine(components.Take(3).ToArray());

            return new[] { new VFXExpressionRGBtoHSV(rgb) };
        }
    }
}
