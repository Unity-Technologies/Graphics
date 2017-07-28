using System;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorCeil : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Ceil"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            // ceil(x) = -floor(-x)
            return new[] { VFXOperatorUtility.Negate(new VFXExpressionFloor(VFXOperatorUtility.Negate(inputExpression[0]))) };
        }
    }
}
