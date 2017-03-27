using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorSubtract : VFXOperatorBinaryFloatOperationZero
    {
        override public string name { get { return "Subtract"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSubtract(inputExpression[0], inputExpression[1]) };
        }
    }
}

