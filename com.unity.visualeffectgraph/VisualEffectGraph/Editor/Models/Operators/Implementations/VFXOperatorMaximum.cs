using System;

namespace UnityEditor.VFX
{
	[VFXInfo(category = "Math")]
	class VFXOperatorMaximum : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Maximum"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMax(inputExpression[0], inputExpression[1]) };
        }
    }
}
