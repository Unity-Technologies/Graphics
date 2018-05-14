using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class OneMinus : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        public override sealed string name { get { return "One Minus (1-x)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var input = inputExpression[0];
            var one = VFXOperatorUtility.OneExpression[input.valueType];
            return new[] { one - input };
        }

        protected sealed override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }
    }
}
