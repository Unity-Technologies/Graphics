using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Utility")]
    class VFXOperatorRectangularToPolar : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The 2D coordinate to be converted into Polar space.")]
            public Vector2 coordinate = Vector2.zero;
        }
        public class OutputProperties
        {
            [Tooltip("The angular coordinate (Polar angle).")]
            public float theta = 45.0f;
            [Tooltip("The radial coordinate (Radius).")]
            public float distance = 1.0f;
        }

        override public string name { get { return "Rectangular to Polar"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var results = VFXOperatorUtility.RectangularToPolar(inputExpression[0]);
            results[0] = VFXOperatorUtility.RadToDeg(results[0]);
            return results;
        }
    }
}
