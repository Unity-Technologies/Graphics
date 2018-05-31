namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Power : VFXOperatorNumericCascadedUnified
    {
        public override sealed string name { get { return "Power"; } }
        protected override sealed double defaultValueDouble { get { return 1.0; } }
        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionPow(a, b);
        }
    }
}
