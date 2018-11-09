using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class LengthDeprecated : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            [Tooltip("The vector to be used in the length calculation.")]
            public FloatN x = Vector3.one;
        }

        public class OutputProperties
        {
            [Tooltip("The length of x.")]
            public float l;
        }

        override public string name { get { return "Length (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(Length));
        }
    }
}
