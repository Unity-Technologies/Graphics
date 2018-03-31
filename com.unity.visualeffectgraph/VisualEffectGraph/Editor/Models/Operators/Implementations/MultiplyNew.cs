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
        override public string name { get { return "MultiplyNew"; } }

        protected override double defaultValueDouble { get { return 1.0; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] * inputExpression[1] };
        }
    }

    //TODOPAUL : Move them to another file (work until we don't deserialize it)
    [VFXInfo(category = "Math")]
    class AddNew : VFXOperatorNumericCascadedUnifiedNew
    {
        override public string name { get { return "AddNew"; } }
        protected override double defaultValueDouble { get { return 0.0; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] + inputExpression[1] };
        }
    }

    [VFXInfo(category = "Math")]
    class MinimumNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "MinimumNew"; } }

        protected override sealed double defaultValueDouble { get { throw new NotImplementedException(); } }
        protected override sealed float defaultValueFloat { get { return float.MaxValue; } }
        protected override sealed int defaultValueInt { get { return int.MaxValue; } }
        protected override sealed uint defaultValueUint { get { return uint.MaxValue; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMin(inputExpression[0], inputExpression[1]) };
        }
    }


    [VFXInfo(category = "Math")]
    class MaximumNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "MaximumNew"; } }

        protected override sealed double defaultValueDouble { get { throw new NotImplementedException(); } }
        protected override sealed float defaultValueFloat { get { return float.MinValue; } }
        protected override sealed int defaultValueInt { get { return int.MinValue; } }
        protected override sealed uint defaultValueUint { get { return uint.MinValue; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionMin(inputExpression[0], inputExpression[1]) };
        }
    }

    [VFXInfo(category = "Math")]
    class PowerNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "PowerNew"; } }
        protected override sealed double defaultValueDouble { get { return 1.0; } }
        protected sealed override bool allowInteger { get { return false; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionPow(inputExpression[0], inputExpression[1]) };
        }
    }
}
