using System;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXOperatorFloat4 : VFXOperator
    {
        override public string name { get { return "Temp_Float4"; } }
        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXValueFloat4.Default };
        }
    }
}


