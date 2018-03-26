using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Bitwise")]
    class VFXOperatorBitwiseRightShift : VFXOperator
    {
        override public string name { get { return "Right Shift"; } }

        public class InputProperties
        {
            static public uint FallbackValue = 0;
            [Tooltip("The first operand.")]
            public uint a = FallbackValue;
            [Tooltip("The second operand.")]
            public uint b = FallbackValue;
        }

        public class OutputProperties
        {
            public uint o;
        }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionBitwiseRightShift(inputExpression[0], inputExpression[1]) };
        }
    }
}
