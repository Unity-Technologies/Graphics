
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class PowerNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "PowerNew"; } }
        protected override sealed double defaultValueDouble { get { return 1.0; } }
        protected override sealed bool allowInteger { get { return false; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionPow(inputExpression[0], inputExpression[1]) };
        }
    }
}

