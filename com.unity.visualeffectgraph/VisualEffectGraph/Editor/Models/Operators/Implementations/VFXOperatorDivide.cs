using System;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorDivide : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Divide"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] / inputExpression[1] };
        }
    }
}
