using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class SineWaveDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            public FloatN input = 0.5f;
            public FloatN frequency = 1.0f;
        }

        override public string name { get { return "Sine Wave (deprecated)"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            //(1-cos(F*2*pi*x))/2
            var type = inputExpression[0].valueType;
            var one = VFXOperatorUtility.OneExpression[type];
            var tau = VFXOperatorUtility.TauExpression[type];
            var two = VFXOperatorUtility.TwoExpression[type];

            return new[] { new VFXExpressionDivide(one - new VFXExpressionCos(inputExpression[0] * inputExpression[1] * tau), two) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(SineWave));
        }
    }
}
