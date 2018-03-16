using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Bitwise")]
    class VFXOperatorBitwiseXor : VFXOperatorBinaryUintOperationZero
    {
        override public string name { get { return "Xor"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseXor(inputExpression[0], inputExpression[1]) };
        }
    }
}
