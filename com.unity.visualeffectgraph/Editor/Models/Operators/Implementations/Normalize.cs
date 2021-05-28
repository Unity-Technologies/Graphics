using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class SafeNormalizationVariantProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "safeNormalize", new object[]{true, false} }
                };
            }
        }
    }


    [VFXInfo(category = "Math/Vector", variantProvider = typeof(SafeNormalizationVariantProvider))]
    class Normalize : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public Vector3 x = Vector3.one;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies if the operator should check if the vector to be normalized is a vero vector.")]
        bool safeNormalize = false;


        protected override sealed string operatorName { get {return  safeNormalize ?  "Safe Normalize" :  "Normalize"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if(safeNormalize)
                return new[] { VFXOperatorUtility.SafeNormalize(inputExpression[0]) };
            else
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
