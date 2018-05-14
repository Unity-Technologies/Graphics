using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp", experimental = true)]
    class Saturate : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Tooltip("The value to be clamped.")]
            public float input = 0.0f;
        }

        public override sealed string name { get { return "SaturateNew"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Saturate(inputExpression[0]) };
        }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }
    }
}
