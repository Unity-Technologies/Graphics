namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Atan2")]
    [VFXInfo(category = "Math/Trigonometry")]
    class Atan2 : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 1.0f;
            public float y = 0.0f;
        }

        protected override sealed string operatorName { get { return "Atan2"; } }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionATan2(inputExpression[1], inputExpression[0]) };
        }
    }
}
