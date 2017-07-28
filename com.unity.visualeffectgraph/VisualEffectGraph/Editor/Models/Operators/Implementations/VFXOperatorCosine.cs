using System;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorCosine : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Cosine"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }
    }
}
