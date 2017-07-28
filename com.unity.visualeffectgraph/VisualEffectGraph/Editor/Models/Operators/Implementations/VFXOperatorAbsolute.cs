using System;
namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorAbsolute : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Absolute"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAbs(inputExpression[0]) };
        }
    }
}
