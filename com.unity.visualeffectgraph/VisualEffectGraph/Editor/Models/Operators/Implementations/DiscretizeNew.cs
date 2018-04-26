using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp", experimental = true)]
    class DiscretizeNew : VFXOperatorNumericUnifiedNew, IVFXOperatorNumericUnifiedConstrained
    {
        public class InputProperties
        {
            [Tooltip("The value to be discretized.")]
            public float a = 0.0f;
            [Min(0.000001f), Tooltip("The granularity.")]
            public float b = 1.0f;
        }

        public override sealed string name { get { return "DiscretizeNew"; } }

        public IEnumerable<int> strictSameTypeSlotIndex
        {
            get
            {
                return Enumerable.Range(0, 2);
            }
        }

        public IEnumerable<int> allowExceptionalScalarSlotIndex
        {
            get
            {
                yield return 1;
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
            return new[] { VFXOperatorUtility.Discretize(inputExpression[0], inputExpression[1]) };
        }
    }
}
