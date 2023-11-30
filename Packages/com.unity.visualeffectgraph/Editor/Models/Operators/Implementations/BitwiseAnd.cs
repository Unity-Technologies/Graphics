using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-BitwiseAnd")]
    [VFXInfo(name = "And", category = "Bitwise")]
    class BitwiseAnd : VFXOperator
    {
        override public string name { get { return "And"; } }

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
            [Tooltip("Outputs the result of the logical AND operation between the two operands. For example, 5 (0101 in binary) with 3 (0011 in binary) returns 1 (0001 in binary).")]
            public uint o = 0;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseAnd(inputExpression[0], inputExpression[1]) };
        }
    }
}
