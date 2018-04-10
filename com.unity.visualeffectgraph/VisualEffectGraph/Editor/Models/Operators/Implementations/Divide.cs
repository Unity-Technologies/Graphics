using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class Divide : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Divide"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] / inputExpression[1] };
        }
    }
}
