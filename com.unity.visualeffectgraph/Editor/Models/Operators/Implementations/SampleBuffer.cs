using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling")]
    class SampleBuffer : VFXOperatorDynamicType
    {
        public override IEnumerable<int> staticSlotIndex
        {
            get
            {
                yield return 0;
                yield return 1;
            }
        }

        public override IEnumerable<Type> validTypes
        {
            get
            {
                //TODOPAUL : filter only ones declared with a flag
                return VFXLibrary.GetSlotsType();
            }
        }


        protected override Type defaultValueType => null;

        override public string name { get { return "Sample Graphics Buffer"; } }

        public class InputProperties
        {
            [Tooltip("Sets the Signed Distance Field texture to sample from.")]
            public GraphicsBuffer buffer = null;
            [Tooltip("Sets the oriented box containing the SDF.")]
            public uint index;
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                if (m_Type != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(m_Type, "s"));
            }
        }
        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (GetNbOutputSlots() == 0)
                return new VFXExpression[] {};

            var outputSlot = GetOutputSlot(0);
            var slots = outputSlot.GetVFXValueTypeSlots();

            var expressions = new List<VFXExpression>();
            foreach (var slot in slots)
            {
                var current = new VFXExpressionSampleBuffer(m_Type, slot.valueType, slot.name, inputExpression[0], inputExpression[1]);
                expressions.Add(current);
            }
            return expressions.ToArray();
        }
    }
}
