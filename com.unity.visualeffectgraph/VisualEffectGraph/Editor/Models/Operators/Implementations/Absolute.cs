using System;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class Absolute : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x;
        }

        public override sealed string name { get { return "Absolute"; } }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptUnsignedInteger;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAbs(inputExpression[0]) };
        }
    }
}
