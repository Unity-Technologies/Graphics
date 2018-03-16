using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Bitwise")]
    class VFXOperatorBitwiseComplement : VFXOperatorUnaryUintOperation
    {
        override public string name { get { return "Complement"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseComplement(inputExpression[0]) };
        }
    }
}
