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

        override public string name { get { return "Rectangular to Polar"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var results = VFXOperatorUtility.RectangularToPolar(inputExpression[0]);
            results[0] = VFXOperatorUtility.RadToDeg(results[0]);
            return results;
        }
    }
}
