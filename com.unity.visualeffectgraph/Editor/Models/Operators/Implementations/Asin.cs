using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Trigonometry")]
    class Asin : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Range(-1.0f, 1.0f)]
            public float x = 0.0f;
        }

        protected override sealed string operatorName { get { return "Asin"; } }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionASin(inputExpression[0]) };
        }
    }
}
