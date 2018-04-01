using System;
using UnityEditor.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class SubtractNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "SubtractNew"; } }
        protected override sealed double defaultValueDouble { get { return 0.0; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] - inputExpression[1] };
        }
    }
}

