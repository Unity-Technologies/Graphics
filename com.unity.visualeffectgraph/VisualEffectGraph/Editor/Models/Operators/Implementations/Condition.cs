using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Flow")]
    class Condition : VFXOperator
    {
        [VFXSetting, SerializeField]
        protected VFXCondition condition = VFXCondition.Equal;

        public class InputProperties
        {
            [Tooltip("The left operand.")]
            public float left = 0.0f;
            [Tooltip("The right operand.")]
            public float right = 0.0f;
        }

        public class OutputProperties
        {
            [Tooltip("The result of the comparison.")]
            public bool res;
        }

        override public string name { get { return "Compare"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCondition(condition, inputExpression[0], inputExpression[1]) };
        }
    }
}
