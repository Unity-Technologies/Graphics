using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-BitwiseLeftShift")]
    [VFXInfo(category = "Bitwise")]
    class BitwiseLeftShift : VFXOperator
    {
        override public string name { get { return "Left Shift"; } }

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
            [Tooltip("Outputs the result of the logical << operation between the two operands. For example, 2 (0010 in binary) left shifted by 1 returns 4 (0100 in binary).")]
            public uint o = 0;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseLeftShift(inputExpression[0], inputExpression[1]) };
        }
    }
}
