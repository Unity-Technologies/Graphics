using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class SquareWaveDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            public FloatN input = 0.5f;
            public FloatN frequency = 1.0f;
        }

        override public string name { get { return "Square Wave (deprecated)"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            //round(frac(x*F))
            var expression = inputExpression[0] * inputExpression[1];
            return new[] { VFXOperatorUtility.Round(VFXOperatorUtility.Frac(expression)) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(SquareWave));
        }
    }
}
