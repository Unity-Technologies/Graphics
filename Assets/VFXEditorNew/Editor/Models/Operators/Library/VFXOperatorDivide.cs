using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorDivide : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Divide"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionDivide(inputExpression[0], inputExpression[1]) };
        }
    }
}

