using System;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic", experimental = true)]
    class AddNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "AddNew"; } }
        protected override sealed double defaultValueDouble { get { return 0.0; } }
        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a + b;
        }
    }
}
