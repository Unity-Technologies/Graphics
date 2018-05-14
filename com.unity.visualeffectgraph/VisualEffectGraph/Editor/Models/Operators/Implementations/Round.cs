using System;
using UnityEditor.VFX;
namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Round : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x = 0.5f;
        }

        public override sealed string name { get { return "Round"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Round(inputExpression[0]) };
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
