using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class SaturateDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The value to be clamped.")]
            public FloatN input = new FloatN(0.0f);
        }

        override public string name { get { return "Saturate"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Saturate(inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Saturate));
        }
    }
}
