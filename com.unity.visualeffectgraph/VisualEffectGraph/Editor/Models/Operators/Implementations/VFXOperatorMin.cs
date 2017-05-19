using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorMin : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Min"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMin(inputExpression[0], inputExpression[1]) };
        }
    }
}
