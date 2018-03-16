using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Bitwise")]
    class VFXOperatorBitwiseAnd : VFXOperatorBinaryUintOperationZero
    {
        override public string name { get { return "And"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseAnd(inputExpression[0], inputExpression[1]) };
        }
    }
}
