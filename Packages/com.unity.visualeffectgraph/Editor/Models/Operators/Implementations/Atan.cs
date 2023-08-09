namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Atan")]
    [VFXInfo(category = "Math/Trigonometry")]
    class Atan : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        protected override sealed string operatorName { get { return "Atan"; } }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionATan(inputExpression[0]) };
        }
    }
}
