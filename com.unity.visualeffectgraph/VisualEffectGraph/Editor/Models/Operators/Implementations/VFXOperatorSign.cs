using System;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorSign : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Sign"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSign(inputExpression[0]) };
        }
    }
}
