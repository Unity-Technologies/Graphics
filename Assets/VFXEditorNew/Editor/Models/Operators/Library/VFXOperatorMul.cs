using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorMul : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Mul"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMul(inputExpression[0], inputExpression[1]) };
        }
    }
}
