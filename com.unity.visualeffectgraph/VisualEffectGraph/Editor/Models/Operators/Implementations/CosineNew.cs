namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class CosineNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        public override sealed string name { get { return "CosineNew"; } }

        protected override sealed bool allowInteger { get { return false; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }
    }
}
