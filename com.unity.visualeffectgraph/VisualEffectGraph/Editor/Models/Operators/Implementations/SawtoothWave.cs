using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Wave")]
    class SawtoothWave : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            public FloatN input = 0.5f;
            public FloatN frequency = 1.0f;
        }

        override public string name { get { return "Sawtooth Wave"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            // abs(frac(x*F))
            return new[] { new VFXExpressionAbs(VFXOperatorUtility.Frac(inputExpression[0] * inputExpression[1])) };
        }
    }
}
