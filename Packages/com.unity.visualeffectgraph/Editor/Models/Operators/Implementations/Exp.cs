using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class MathExpVariantProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant($"Exp2", "Math/Exp", typeof(Exp), new[] {new KeyValuePair<string, object>("_base", VFXOperatorUtility.Base.Base2)}, null, new []{ "exponential" });
            yield return new Variant($"Exp10", "Math/Exp", typeof(Exp), new[] {new KeyValuePair<string, object>("_base", VFXOperatorUtility.Base.Base10)}, null, new []{ "exponential" });
            yield return new Variant($"Exp", "Math/Exp", typeof(Exp), new[] {new KeyValuePair<string, object>("_base", VFXOperatorUtility.Base.BaseE)}, null, new []{ "exponential" });
        }
    }

    [VFXHelpURL("Operator-Exp")]
    [VFXInfo(variantProvider = typeof(MathExpVariantProvider))]
    class Exp : VFXOperatorNumericUniform
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the base of the exponential")]
        VFXOperatorUtility.Base _base = VFXOperatorUtility.Base.BaseE;

        public class InputProperties
        {
            public float x = 0.0f;
        }

        protected sealed override string operatorName
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

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Exp(inputExpression[0], _base) };
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
