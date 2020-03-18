using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Log", variantProvider = typeof(MathBaseVariantProvider))]
    class Log : VFXOperatorNumericUniform
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the base of the logarithm")]
        VFXOperatorUtility.Base _base = VFXOperatorUtility.Base.BaseE;

        public class InputProperties
        {
            public float x = 0.0f;
        }

        protected override sealed string operatorName
        {
            get
            {
                switch (_base)
                {
                    case VFXOperatorUtility.Base.Base2: return "Log2";
                    case VFXOperatorUtility.Base.Base10: return "Log10";
                    case VFXOperatorUtility.Base.BaseE: return "Log";
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Log(inputExpression[0], _base) };
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
