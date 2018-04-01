using System;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class AddNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "AddNew"; } }
        protected override sealed double defaultValueDouble { get { return 0.0; } }
        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] + inputExpression[1] };
        }
    }
}
