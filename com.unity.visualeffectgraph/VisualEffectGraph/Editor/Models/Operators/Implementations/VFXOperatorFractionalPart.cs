using System;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorFractionalPart : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Fractional Part"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Frac(inputExpression[0]) };
        }
    }
}
