using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Trigonometry")]
    class Acos : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Range(-1.0f, 1.0f)]
            public float x = 0.0f;
        }

        protected override sealed string operatorName { get { return "Acos"; } }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionACos(inputExpression[0]) };
        }
    }
}
