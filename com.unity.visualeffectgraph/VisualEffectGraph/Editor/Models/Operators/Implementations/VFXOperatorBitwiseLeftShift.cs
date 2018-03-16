using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Bitwise")]
    class VFXOperatorBitwiseLeftShift : VFXOperatorBinaryUintOperationZero
    {
        override public string name { get { return "Left Shift"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseLeftShift(inputExpression[0], inputExpression[1]) };
        }
    }
}
