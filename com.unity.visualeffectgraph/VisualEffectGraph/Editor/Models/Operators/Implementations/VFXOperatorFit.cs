using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorFit : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public FloatN input = new FloatN(0.5f);
            [Tooltip("The start of the old range.")]
            public FloatN oldRangeMin = new FloatN(0.0f);
            [Tooltip("The end of the old range.")]
            public FloatN oldRangeMax = new FloatN(1.0f);
            [Tooltip("The start of the new range.")]
            public FloatN newRangeMin = new FloatN(5.0f);
            [Tooltip("The end of the new range.")]
            public FloatN newRangeMax = new FloatN(10.0f);
        }

        override public string name { get { return "Remap"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Fit(inputExpression[0], inputExpression[1], inputExpression[2], inputExpression[3], inputExpression[4]) };
        }
    }
}
