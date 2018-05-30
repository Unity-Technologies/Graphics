using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class ModuloDeprecated : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The numerator operand.")]
            public FloatN a = new FloatN(1.0f);
            [Tooltip("The denominator operand.")]
            public FloatN b = new FloatN(1.0f);
        }

        override public string name { get { return "Modulo (deprecated)"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Modulo(inputExpression[0], inputExpression[1]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Modulo));
        }
    }
}
