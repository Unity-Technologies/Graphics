using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Bitwise")]
    class BitwiseOr : VFXOperator
    {
        override public string name { get { return "Or"; } }

        public class InputProperties
        {
            static public uint FallbackValue = 0;
            [Tooltip("Sets the first operand.")]
            public uint a = FallbackValue;
            [Tooltip("Sets the second operand.")]
            public uint b = FallbackValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the result of the logical OR operation between the two operands. For example, 5 (0101 in binary) with 3 (0011 in binary) returns 7 (0111 in binary).")]
            public uint o = 0;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseOr(inputExpression[0], inputExpression[1]) };
        }
    }
}
