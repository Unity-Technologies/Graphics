using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class Maximum : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Maximum"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMax(inputExpression[0], inputExpression[1]) };
        }
    }
}
