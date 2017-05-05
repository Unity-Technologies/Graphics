using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorPow : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Pow"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionPow(inputExpression[0], inputExpression[1]) };
        }
    }
}
