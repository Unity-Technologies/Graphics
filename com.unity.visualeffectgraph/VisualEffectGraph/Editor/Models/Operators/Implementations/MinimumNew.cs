using System;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math/Clamp", experimental = true)]
    class MinimumNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "MinimumNew"; } }

        protected override sealed double defaultValueDouble { get { throw new NotImplementedException(); } }
        protected override sealed float defaultValueFloat { get { return float.MaxValue; } }
        protected override sealed int defaultValueInt { get { return int.MaxValue; } }
        protected override sealed uint defaultValueUint { get { return uint.MaxValue; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return new VFXExpressionMin(a, b);
        }
    }
}
