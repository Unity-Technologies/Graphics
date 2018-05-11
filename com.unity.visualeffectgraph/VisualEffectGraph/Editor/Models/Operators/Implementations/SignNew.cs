using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class SignNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        public override sealed string name { get { return "Sign"; } }

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
