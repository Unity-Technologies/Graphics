using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class NormalizeDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The vector to be normalized.")]
            public FloatN x = Vector3.one;
        }

        override public string name { get { return "Normalize (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Normalize(inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Normalize));
        }
    }
}
