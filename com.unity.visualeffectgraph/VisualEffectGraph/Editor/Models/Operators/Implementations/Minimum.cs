using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp")]
    class Minimum : VFXOperatorBinaryFloatOperationOne
    {
        override public string name { get { return "Minimum"; } }

        override protected VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionMin(a, b);
        }
    }
}
