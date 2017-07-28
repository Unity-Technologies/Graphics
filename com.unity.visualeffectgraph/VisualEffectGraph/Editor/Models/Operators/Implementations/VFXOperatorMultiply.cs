using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorMultiply : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Multiply"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMul(inputExpression[0], inputExpression[1]) };
        }
    }
}
