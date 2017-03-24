using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorMax : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Max"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMax(inputExpression[0], inputExpression[1]) };
        }
    }
}

