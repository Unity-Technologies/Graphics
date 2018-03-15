using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorRemapToZeroOne : VFXOperatorFloatUnifiedWithVariadicOutput
    {
        [VFXSetting, Tooltip("Whether the values are clamped to the input/output range")]
        public bool Clamp = false;

        public class InputProperties
        {
            [Tooltip("The value to be remapped into the new range.")]
            public FloatN input = new FloatN(0.0f);
        }

        override public string name { get { return "Remap [-1..1] => [0..1]"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            int size = VFXExpression.TypeToSize(inputExpression[0].valueType);

            VFXExpression input;

            if (Clamp)
                input = VFXOperatorUtility.Clamp(inputExpression[0], VFXOperatorUtility.Negate(VFXOperatorUtility.OneExpression[size]), VFXOperatorUtility.OneExpression[size]);
            else
                input = inputExpression[0];

            var zerofive = VFXOperatorUtility.HalfExpression[size];
            return new[] { VFXOperatorUtility.Mad(input, zerofive, zerofive) };
        }
    }
}
