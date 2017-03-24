using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorFloat3 : VFXOperator
    {
        override public string name { get { return "Temp_Float3"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValueFloat3.Default };
        }
    }
}

