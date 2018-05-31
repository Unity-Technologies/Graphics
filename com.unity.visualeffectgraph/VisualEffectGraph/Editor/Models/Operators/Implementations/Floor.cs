using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Floor : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        public override sealed string name { get { return "Floor"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionFloor(inputExpression[0]) };
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
