using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;


namespace UnityEditor.VFX
{
    [VFXInfo(category = "Math")]
    class VFXOperatorAddNew : VFXOperatorNumericCascadedUnifiedNew
    {
        override public string name { get { return "AddNew"; } }

        protected override double defaultValueDouble { get { return 0.0; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] + inputExpression[1] };
        }
    }
}
