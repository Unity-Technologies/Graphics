using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class Minimum : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Minimum"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMin(inputExpression[0], inputExpression[1]) };
        }
    }
}
