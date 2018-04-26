using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Remap", experimental = true)]
    class RemapToZeroOneNew : VFXOperatorNumericUniformNew
    {
        [VFXSetting, SerializeField, Tooltip("Whether the values are clamped to the input/output range")]
        private bool m_Clamp = false;

        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public float input = 0.0f;
        }

        public override sealed string name { get { return "Remap [-1..1] => [0..1]New"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var type = inputExpression[0].valueType;

            var zerofive = VFXOperatorUtility.HalfExpression[type];
            var expression = VFXOperatorUtility.Mad(inputExpression[0], zerofive, zerofive);

            if (m_Clamp)
                return new[] { VFXOperatorUtility.Saturate(expression) };
            else
                return new[] { expression };
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
