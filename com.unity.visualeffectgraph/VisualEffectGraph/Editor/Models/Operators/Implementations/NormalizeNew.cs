using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Vector", experimental = true)]
    class NormalizeNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public Vector3 x = Vector3.one;
        }

        public override sealed string name { get { return "NormalizeNew"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Normalize(inputExpression[0]) };
        }

        protected override sealed ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowVectorType;
            }
        }
    }
}
