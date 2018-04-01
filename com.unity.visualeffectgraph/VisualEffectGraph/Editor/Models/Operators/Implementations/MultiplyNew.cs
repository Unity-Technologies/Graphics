using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class MultiplyNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "MultiplyNew"; } }

        protected override sealed double defaultValueDouble { get { return 1.0; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] * inputExpression[1] };
        }
    }
}
