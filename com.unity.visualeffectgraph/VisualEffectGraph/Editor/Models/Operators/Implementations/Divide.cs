using System;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Arithmetic")]
    class DivideNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "Divide"; } }

        protected override sealed double defaultValueDouble { get { return 1.0; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a / b;
        }
    }
}
