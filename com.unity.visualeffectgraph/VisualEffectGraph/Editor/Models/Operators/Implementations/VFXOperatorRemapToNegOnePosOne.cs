using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorRemapToNegOnePosOne : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public FloatN input = new FloatN(0.5f);
        }

        override public string name { get { return "Remap [0..1] => [-1..1]"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            int size = VFXExpression.TypeToSize(inputExpression[0].valueType);
            return new[] { VFXOperatorUtility.Fit(inputExpression[0], VFXOperatorUtility.ZeroExpression[size], VFXOperatorUtility.OneExpression[size], VFXOperatorUtility.Negate(VFXOperatorUtility.OneExpression[size]) , VFXOperatorUtility.OneExpression[size]) };
        }
    }
}
