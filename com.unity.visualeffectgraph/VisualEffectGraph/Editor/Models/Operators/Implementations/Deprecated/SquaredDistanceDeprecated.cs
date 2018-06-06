using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class SquaredDistanceDeprecated : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
            [Tooltip("The first operand.")]
            public FloatN a = Vector3.zero;
            [Tooltip("The second operand.")]
            public FloatN b = Vector3.zero;
        }

        public class OutputProperties
        {
            [Tooltip("The squared distance between a and b.")]
            public float d;
        }

        override public string name { get { return "Squared Distance (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.SqrDistance(inputExpression[0], inputExpression[1]) };
        }

        public sealed override void Sanitize()
        {
            base.Sanitize();
            SanitizeHelper.ToOperatorWithoutFloatN(this, typeof(SquaredDistance));
        }
    }
}
