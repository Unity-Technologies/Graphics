using System;
using UnityEngine;


namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorSquareRoot : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Square Root"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Sqrt(inputExpression[0]) };
        }
    }
}
