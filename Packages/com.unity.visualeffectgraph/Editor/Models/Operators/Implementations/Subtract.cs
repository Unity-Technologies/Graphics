namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Subtract")]
    [VFXInfo(category = "Math/Arithmetic", synonyms = new []{ "minus" })]
    class Subtract : VFXOperatorNumericCascadedUnified
    {
        protected sealed override string operatorName => "Subtract";
        protected sealed override double defaultValueDouble => 0.0;

        protected sealed override VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a - b;
        }
    }
}
