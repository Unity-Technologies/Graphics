using System;
using UnityEngine;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorModulo : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
			[Tooltip("The numerator operand.")]
			public FloatN a = new FloatN(1.0f);
			[Tooltip("The denominator operand.")]
			public FloatN b = new FloatN(1.0f);
        }

        override public string name { get { return "Modulo"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Fmod(inputExpression[0], inputExpression[1]) };
        }
    }
}
