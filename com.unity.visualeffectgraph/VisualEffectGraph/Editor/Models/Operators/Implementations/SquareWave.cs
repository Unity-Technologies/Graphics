using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Wave")]
    class SquareWave : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            public FloatN input = 0.5f;
            public FloatN frequency = 1.0f;
        }

        override public string name { get { return "Square Wave"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            //round(frac(x*F))
            var expression = inputExpression[0] * inputExpression[1];
            return new[] { VFXOperatorUtility.Round(VFXOperatorUtility.Frac(expression)) };
        }
    }
}
