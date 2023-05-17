namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Cosine")]
    [VFXInfo(category = "Math/Trigonometry")]
    class Cosine : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        protected override sealed string operatorName { get { return "Cosine"; } }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptIntegerAndDirection; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }
    }
}
