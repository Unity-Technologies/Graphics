using System;
using System.Linq;
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
                foreach (var type in VFXLibrary.GetSlotsType())
                {
                    var typeAttribute = type.GetCustomAttributes(typeof(VFXTypeAttribute), true).FirstOrDefault() as VFXTypeAttribute;
                    if (typeAttribute != null && typeAttribute.flags.HasFlag(VFXTypeAttribute.Flags.GraphicsBuffer))
                        yield return type;
                }
            }
        }


        protected override Type defaultValueType => validTypes.FirstOrDefault();

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

            var buffer = inputExpression[0];
            var index = inputExpression[1];
            var stride = new VFXExpressionBufferStride(buffer);
            var count = new VFXExpressionBufferCount(buffer);

            var expressions = new List<VFXExpression>();
            foreach (var slot in slots)
            {
                var current = new VFXExpressionSampleBuffer(m_Type, slot.valueType, slot.name, buffer, index, stride, count);
                expressions.Add(current);
            }
            return expressions.ToArray();
        }
    }
}
