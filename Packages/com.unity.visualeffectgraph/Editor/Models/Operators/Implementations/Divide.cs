namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Divide")]
    [VFXInfo(category = "Math/Arithmetic")]
    class Divide : VFXOperatorNumericCascadedUnified
    {
        protected override sealed string operatorName { get { return "Divide"; } }

        protected override sealed double defaultValueDouble { get { return 1.0; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a / b;
        }
    }
}
