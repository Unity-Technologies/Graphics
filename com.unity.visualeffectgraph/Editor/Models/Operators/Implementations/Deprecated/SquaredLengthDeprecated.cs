using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class SquaredLengthDeprecated : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            [Tooltip("The vector used to calculate the squared length.")]
            public FloatN x = Vector3.one;
        }

        public class OutputProperties
        {
            [Tooltip("The squared length of x.")]
            public float l;
        }

        override public string name { get { return "Squared Length (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Dot(inputExpression[0], inputExpression[0]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(SquaredLength));
        }
    }
}
