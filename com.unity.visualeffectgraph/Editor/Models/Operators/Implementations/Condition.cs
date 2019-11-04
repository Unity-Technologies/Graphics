using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Logic")]
    class Condition : VFXOperator
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the comparison condition between the Left and Right operands.")]
        protected VFXCondition condition = VFXCondition.Equal;

        public class InputProperties
        {
            [Tooltip("Sets the left operand which will be compared to the right operand based on the specified condition.")]
            public float left = 0.0f;
            [Tooltip("Sets the right operand which will be compared to the left operand based on the specified condition.")]
            public float right = 0.0f;
        }

        public class OutputProperties
        {
            [Tooltip("The result of the comparison.")]
            public bool o;
        }

        override public string name { get { return "Compare"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCondition(condition, inputExpression[0], inputExpression[1]) };
        }
    }
}
