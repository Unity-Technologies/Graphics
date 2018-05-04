using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic", experimental = true)]
    class DivideNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "DivideNew"; } }

        protected override sealed double defaultValueDouble { get { return 1.0; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a / b;
        }
    }
}
