using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp", experimental = true)]
    class CeilingNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x;
        }

        public override sealed string name { get { return "CeilingNew"; } }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Ceil(inputExpression[0]) };
        }
    }
}
