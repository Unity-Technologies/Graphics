namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Ceiling")]
    [VFXInfo(category = "Math/Clamp")]
    class Ceiling : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0;
        }

        protected override sealed string operatorName { get { return "Ceiling"; } }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptIntegerAndDirection;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Ceil(inputExpression[0]) };
        }
    }
}
