using System;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorPower : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Power"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionPow(inputExpression[0], inputExpression[1]) };
        }
    }
}
