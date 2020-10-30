using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Logic")]
    class Condition : VFXOperatorDynamicType
    {
        [VFXSetting, SerializeField, Tooltip("Specifies the comparison condition between the Left and Right operands.")]
        protected VFXCondition condition = VFXCondition.Equal;

        public class OutputProperties
        {
            [Tooltip("The result of the comparison.")]
            public bool o;
        }

        override public string name { get { return "Compare"; } }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(
                    new VFXProperty(GetOperandType(), "left", new TooltipAttribute("Sets the left operand which will be compared to the right operand based on the specified condition.")),
                    GetDefaultValueForType(GetOperandType()));
                yield return new VFXPropertyWithValue(
                    new VFXProperty(GetOperandType(), "right", new TooltipAttribute("Sets the right operand which will be compared to the left operand based on the specified condition.")),
                    GetDefaultValueForType(GetOperandType()));
            }
        }

        public override IEnumerable<int> staticSlotIndex => Enumerable.Empty<int>();

        public override IEnumerable<Type> validTypes => new[]
        {
            typeof(float),
            typeof(uint),
            typeof(int),
        };

        protected override Type defaultValueType => typeof(float);

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionCondition(VFXExpression.GetVFXValueTypeFromType(GetOperandType()), condition, inputExpression[0], inputExpression[1]) };
        }
    }
}
