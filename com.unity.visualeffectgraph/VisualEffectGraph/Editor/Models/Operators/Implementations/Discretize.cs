using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class Discretize : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The value to be discretized.")]
            public FloatN a = 0.0f;
            [Min(0.000001f), Tooltip("The granularity.")]
            public FloatN b = 1.0f;
        }

        override public string name { get { return "Discretize (deprecated)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Discretize(inputExpression[0], inputExpression[1]) };
        }

        public sealed override void Sanitize()
        {
            SanitizeHelper.SanitizeToOperatorNew(this, typeof(DiscretizeNew));
        }
    }
}
