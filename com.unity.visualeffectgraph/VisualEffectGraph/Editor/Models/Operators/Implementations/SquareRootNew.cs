using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic", experimental = true)]
    class SquareRootNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        public override sealed string name { get { return "Square RootNew"; } }

        protected override sealed ValidTypeRule typeFilter { get { return ValidTypeRule.allowEverythingExceptInteger; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Sqrt(inputExpression[0]) };
        }
    }
}
