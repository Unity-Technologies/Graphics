using System;
using UnityEngine;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Vector")]
	class VFXOperatorSquaredDistance : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
			[Tooltip("The first operand.")]
			public FloatN a = Vector3.zero;
			[Tooltip("The second operand.")]
			public FloatN b = Vector3.zero;
        }

        override public string name { get { return "Squared Distance"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.SqrDistance(inputExpression[0], inputExpression[1]) };
        }
    }
}
