using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;


namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Math")]
    class MultiplyNew :  VFXOperatorNumericCascadedUnifiedNew
    {
        override public string name { get { return "MultiplyNew"; } }

        protected override double defaultValueDouble { get { return 1.0; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] * inputExpression[1] };
        }
    }
}
