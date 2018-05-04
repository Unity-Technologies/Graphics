using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic", experimental = true)]
    class SignNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        public override sealed string name { get { return "SignNew"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSign(inputExpression[0]) };
        }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptUnsignedInteger;
            }
        }
    }
}
