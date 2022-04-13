using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Remap")]
    class Remap : VFXOperatorNumericUnified, IVFXOperatorNumericUnifiedConstrained
    {
        [VFXSetting, SerializeField, Tooltip("When enabled, the input value is clamped between the min and max of the old range.")]
        private bool m_Clamp = false;

        public class InputProperties
        {
            [Tooltip("Sets the value to be remapped into the new range.")]
            public float input = 0.5f;
            [Tooltip("Sets the start of the old input range.")]
            public float oldRangeMin = 0.0f;
            [Tooltip("Sets the end of the old input range.")]
            public float oldRangeMax = 1.0f;
            [Tooltip("Sets the start of the new remapped range.")]
            public float newRangeMin = 5.0f;
            [Tooltip("Sets the end of the new remapped range.")]
            public float newRangeMax = 10.0f;
        }

        protected override sealed string operatorName { get { return "Remap"; } }

        public IEnumerable<int> slotIndicesThatMustHaveSameType
        {
            get
            {
                return Enumerable.Range(0, 5);
            }
        }

        public IEnumerable<int> slotIndicesThatCanBeScalar
        {
            get
            {
                return Enumerable.Range(1, 4);
            }
        }

        protected sealed override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression input;
            if (m_Clamp)
                input = VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]);
            else
                input = inputExpression[0];

            return new[] { VFXOperatorUtility.Fit(input, inputExpression[1], inputExpression[2], inputExpression[3], inputExpression[4]) };
        }
    }
}
