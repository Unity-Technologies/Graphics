using System;
using UnityEngine;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Vector")]
	class VFXOperatorSquaredLength : VFXOperatorFloatUnified
    {
        public class InputProperties
        {
			[Tooltip("The vector used to calculate the squared length.")]
			public FloatN x = Vector3.one;
        }

        override public string name { get { return "Squared Length"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Dot(inputExpression[0], inputExpression[0]) };
        }
    }
}
