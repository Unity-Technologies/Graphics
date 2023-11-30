using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-BitwiseXor")]
    [VFXInfo(name = "Xor", category = "Bitwise")]
    class BitwiseXor : VFXOperator
    {
        override public string name { get { return "Xor"; } }

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
            [Tooltip("Outputs the result of the logical XOR operation between the two operands. For example, 2 (0010 in binary) with 10 (1010 in binary) returns 8 (1000 in binary).")]
            public uint o = 0;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseXor(inputExpression[0], inputExpression[1]) };
        }
    }
}
