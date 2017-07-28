using System;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorTangent : VFXOperatorUnaryFloatOperation
    {
        override public string name { get { return "Tangent"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionTan(inputExpression[0]) };
        }
    }
}
