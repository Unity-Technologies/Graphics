using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Remap(01)")]
    [VFXInfo(category = "Math/Remap")]
    class RemapToZeroOne : VFXOperatorNumericUniform
    {
        [VFXSetting, SerializeField, Tooltip("When enabled, the input value is clamped between -1 and 1.")]
        private bool m_Clamp = false;

        public class InputProperties
        {
            [Tooltip("Sets the value to be remapped into the [0..1] range. If the input value is not clamped, the remapped value can go beyond that range.")]
            public float input = 0.0f;
        }

        protected override sealed string operatorName { get { return "Remap [-1..1] => [0..1]"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var type = inputExpression[0].valueType;

            var half = VFXOperatorUtility.HalfExpression[type];
            var expression = VFXOperatorUtility.Mad(inputExpression[0], half, half);

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
