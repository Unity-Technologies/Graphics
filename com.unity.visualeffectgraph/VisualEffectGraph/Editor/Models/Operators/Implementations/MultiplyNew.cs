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
    //=========== Cascaded
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
    class SubtractNew : VFXOperatorNumericCascadedUnifiedNew
    {
        override public string name { get { return "SubtractNew"; } }
        protected override double defaultValueDouble { get { return 0.0; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { inputExpression[0] - inputExpression[1] };
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
        protected override sealed bool allowInteger { get { return false; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionPow(inputExpression[0], inputExpression[1]) };
        }
    }

    //=========== Unified
    [VFXInfo(category = "Math")]
    class LengthNew : VXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            [Tooltip("The vector to be used in the length calculation.")]
            public Vector3 x = Vector3.one;
        }

        public class OutputProperties
        {
            [Tooltip("The length of x.")]
            public float l;
        }

        protected override bool allowInteger { get { return false; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }

    //=========== Uniform
    [VFXInfo(category = "Math")]
    class ClampNew : VXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            [Tooltip("The value to be clamped.")]
            public float input = 0.0f;
            [Tooltip("The lower bound to clamp the input to.")]
            public float min = 0.0f;
            [Tooltip("The upper bound to clamp the input to.")]
            public float max = 1.0f;
        }

        public override sealed string name { get { return "ClampNew"; } }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }

    [VFXInfo(category = "Math")]
    class LerpNew : VXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            [Tooltip("The start value.")]
            public float x = 0.0f;
            [Tooltip("The end value.")]
            public float y = 1.0f;
            [Tooltip("The amount to interpolate between x and y (0-1).")]
            public float s = 0.5f;
        }

        public override sealed string name { get { return "LerpNew"; } }

        protected override bool allowInteger { get { return false; } }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
