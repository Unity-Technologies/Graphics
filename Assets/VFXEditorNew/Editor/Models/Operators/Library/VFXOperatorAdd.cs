using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorAdd : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Add"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionAdd(inputExpression[0], inputExpression[1]) };
        }
    }
}
