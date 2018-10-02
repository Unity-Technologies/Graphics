using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class RemapDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        [VFXSetting, Tooltip("Whether the values are clamped to the input/output range")]
        public bool Clamp = false;

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

        override public string name { get { return "Remap (deprecated)"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression input;
            if (Clamp)
                input = VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]);
            else
                input = inputExpression[0];

            return new[] { VFXOperatorUtility.Fit(input, inputExpression[1], inputExpression[2], inputExpression[3], inputExpression[4]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Remap));
        }
    }
}
