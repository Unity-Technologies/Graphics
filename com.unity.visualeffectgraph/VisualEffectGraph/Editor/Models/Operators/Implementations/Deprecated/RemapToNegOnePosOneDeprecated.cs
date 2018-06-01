using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class RemapToNegOnePosOneDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        [VFXSetting, Tooltip("Whether the values are clamped to the input/output range")]
        public bool Clamp = false;

        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public FloatN input = new FloatN(0.5f);
        }

        override public string name { get { return "Remap [0..1] => [-1..1] (deprecated)"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var type = inputExpression[0].valueType;

            VFXExpression input;

            if (Clamp)
                input = VFXOperatorUtility.Saturate(inputExpression[0]);
            else
                input = inputExpression[0];

            return new[] { VFXOperatorUtility.Mad(input, VFXOperatorUtility.TwoExpression[type], VFXOperatorUtility.Negate(VFXOperatorUtility.OneExpression[type])) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(RemapToNegOnePosOne));
        }
    }
}
