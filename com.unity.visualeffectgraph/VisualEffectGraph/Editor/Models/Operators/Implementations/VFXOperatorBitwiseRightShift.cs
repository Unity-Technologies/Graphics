using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Bitwise")]
    class VFXOperatorBitwiseRightShift : VFXOperatorBinaryUintOperationZero
    {
        override public string name { get { return "Right Shift"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseRightShift(inputExpression[0], inputExpression[1]) };
        }
    }
}
