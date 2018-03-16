using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Bitwise")]
    class VFXOperatorBitwiseOr : VFXOperatorBinaryUintOperationZero
    {
        override public string name { get { return "Or"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseOr(inputExpression[0], inputExpression[1]) };
        }
    }
}
