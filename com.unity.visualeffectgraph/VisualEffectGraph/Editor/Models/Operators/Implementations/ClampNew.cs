using System;
using UnityEngine;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp", experimental = true)]
    class ClampNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            [Tooltip("The value to be clamped.")]
            public float input = 0.0f;
            [Tooltip("The lower bound to clamp the input to.")]
            public float min = 0.0f;
            [Tooltip("The upper bound to clamp the input to.")]
            public float max = 1.0f;
        }

        public override sealed string name { get { return "ClampNew"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2], false) };
        }
    }
}
