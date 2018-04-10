using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Wave")]
    class SineWave : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            public FloatN input = 0.5f;
            public FloatN frequency = 1.0f;
        }

        override public string name { get { return "Sine Wave"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            //(1-cos(F*2*pi*x))/2
            var size = VFXExpression.TypeToSize(inputExpression[0].valueType);
            var one = VFXOperatorUtility.OneExpression[size];
            var tau = VFXOperatorUtility.TauExpression[size];
            var two = VFXOperatorUtility.TwoExpression[size];

            return new[] { new VFXExpressionDivide(one - new VFXExpressionCos(inputExpression[0] * inputExpression[1] * tau), two) };
        }
    }
}
