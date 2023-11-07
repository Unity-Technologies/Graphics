using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class MathLogVariantProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant($"Log2", "Math/Log", typeof(Log), new[] {new KeyValuePair<string, object>("_base", VFXOperatorUtility.Base.Base2)}, null, new []{ "Logarithm" });
            yield return new Variant($"Log10", "Math/Log", typeof(Log), new[] {new KeyValuePair<string, object>("_base", VFXOperatorUtility.Base.Base10)}, null, new []{ "Logarithm" });
            yield return new Variant($"Log", "Math/Log", typeof(Log), new[] {new KeyValuePair<string, object>("_base", VFXOperatorUtility.Base.BaseE)}, null, new []{ "Logarithm" });
        }
    }

    [VFXHelpURL("Operator-Log")]
    [VFXInfo(variantProvider = typeof(MathLogVariantProvider))]
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
                return ValidTypeRule.allowEverythingExceptIntegerAndDirection;
            }
        }
    }
}
