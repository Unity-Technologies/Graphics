using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Maximum : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Maximum"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionMax(a, b);
        }
    }
}
