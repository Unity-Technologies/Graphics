using System;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math", experimental = true)]
    class MaximumNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "MaximumNew"; } }

        protected override sealed double defaultValueDouble { get { throw new NotImplementedException(); } }
        protected override sealed float defaultValueFloat { get { return float.MinValue; } }
        protected override sealed int defaultValueInt { get { return int.MinValue; } }
        protected override sealed uint defaultValueUint { get { return uint.MinValue; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMax(inputExpression[0], inputExpression[1]) };
        }
    }
}
