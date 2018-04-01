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

    //TODOPAUL : Move them to another file (work until we don't deserialize it)
    //=========== Cascaded
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

    [VFXInfo(category = "Math")]
    class MinimumNew : VFXOperatorNumericCascadedUnifiedNew
    {
        public override sealed string name { get { return "MinimumNew"; } }

        protected override sealed double defaultValueDouble { get { throw new NotImplementedException(); } }
        protected override sealed float defaultValueFloat { get { return float.MaxValue; } }
        protected override sealed int defaultValueInt { get { return int.MaxValue; } }
        protected override sealed uint defaultValueUint { get { return uint.MaxValue; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
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

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
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

    //=========== Uniform
    [VFXInfo(category = "Math")]
    class LengthNew : VFXOperatorNumericUniformNew
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

        protected override sealed bool allowInteger { get { return false; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Length(inputExpression[0]) };
        }
    }

    [VFXInfo(category = "Math")]
    class ClampNew : VFXOperatorNumericUniformNew
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

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Clamp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }

    [VFXInfo(category = "Math")]
    class LerpNew : VFXOperatorNumericUniformNew
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

        protected override sealed bool allowInteger { get { return false; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }

    [VFXInfo(category = "Math")]
    class CosineNew : VFXOperatorNumericUniformNew
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        public override sealed string name { get { return "CosineNew"; } }

        protected override sealed bool allowInteger { get { return false; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCos(inputExpression[0]) };
        }
    }

    //=========== Unified
    [VFXInfo(category = "Math")]
    class DotProductNew : VFXOperatorNumericUnifiedNew
    {
        public class InputProperties
        {
            [Tooltip("The first operand.")]
            public Vector3 a = Vector3.zero;
            [Tooltip("The second operand.")]
            public Vector3 b = Vector3.zero;
        }

        public class OutputProperties
        {
            [Tooltip("The dot product between a and b.")]
            public float d;
        }

        public override sealed string name { get { return "DotProductNew"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Dot(inputExpression[0], inputExpression[1]) };
        }
    }

}
