using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class MathBaseVariantProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "_base", Enum.GetValues(typeof(VFXOperatorUtility.Base)).Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Math/Exp", variantProvider = typeof(MathBaseVariantProvider))]
    class Exp : VFXOperatorNumericUniform
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the base of the exponential")]
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
                    case VFXOperatorUtility.Base.Base2: return "Exp2";
                    case VFXOperatorUtility.Base.Base10: return "Exp10";
                    case VFXOperatorUtility.Base.BaseE: return "Exp";
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Exp(inputExpression[0], _base) };
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
