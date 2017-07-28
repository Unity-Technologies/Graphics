using System;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorSine : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sine"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSin(inputExpression[0]) };
        }
    }
}
