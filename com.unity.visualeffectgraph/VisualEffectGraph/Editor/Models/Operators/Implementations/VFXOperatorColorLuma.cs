using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Color")]
    class VFXOperatorColorLuma : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The color used for the luminance calculation.")]
            public Color color = Color.white;
        }

        public class OutputProperties
        {
            [Tooltip("The luminance of the color.")]
            public float luma;
        }

        override public string name { get { return "Color Luma"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.ColorLuma(inputExpression[0]) };
        }
    }
}
