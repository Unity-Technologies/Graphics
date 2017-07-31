using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Color")]
    class VFXOperatorHSVtoRGB : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The Hue, Saturation and Value parameters.")]
            public Vector3 hsv = new Vector3(1.0f, 0.5f, 0.5f);
        }

        override public string name { get { return "HSV to RGB"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionHSVtoRGB(inputExpression[0]) };
        }
    }
}
