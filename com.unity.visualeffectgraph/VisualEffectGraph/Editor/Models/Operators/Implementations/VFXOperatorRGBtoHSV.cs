using System;
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

        override public string name { get { return "RGB to HSV"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionRGBtoHSV(inputExpression[0]) };
        }
    }
}
