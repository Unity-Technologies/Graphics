using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class RemapToZeroOneDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        [VFXSetting, Tooltip("Whether the values are clamped to the input/output range")]
        public bool Clamp = false;

        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public FloatN input = new FloatN(0.0f);
        }

        override public string name { get { return "Remap [-1..1] => [0..1] (deprecated)"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var type = inputExpression[0].valueType;

            var half = VFXOperatorUtility.HalfExpression[type];
            var expression = VFXOperatorUtility.Mad(inputExpression[0], half, half);

            if (Clamp)
                return new[] { VFXOperatorUtility.Saturate(expression) };
            else
                return new[] { expression };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(RemapToZeroOne));
        }
    }
}
