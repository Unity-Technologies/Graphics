using System;
namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorReciprocal : VFXOperatorUnaryFloatOperation
    {
		override public string name { get { return "Reciprocal (1/x)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionDivide(VFXValue.Constant<float>(1.0f), inputExpression[0]) };
        }
    }
}
