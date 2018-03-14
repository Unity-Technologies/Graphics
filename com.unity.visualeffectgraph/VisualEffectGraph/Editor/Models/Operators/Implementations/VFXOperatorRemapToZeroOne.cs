using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorRemapToZeroOne : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public FloatN input = new FloatN(0.0f);
        }

        override public string name { get { return "Remap [-1..1] => [0..1]"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            int size = VFXExpression.TypeToSize(inputExpression[0].valueType);
            return new[] { VFXOperatorUtility.Fit(inputExpression[0], VFXOperatorUtility.Negate(VFXOperatorUtility.OneExpression[size]), VFXOperatorUtility.OneExpression[size],  VFXOperatorUtility.ZeroExpression[size], VFXOperatorUtility.OneExpression[size]) };
        }
    }
}
