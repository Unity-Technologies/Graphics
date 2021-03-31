using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXType]
    struct DummyTest
    {
        public Vector3 position;
        public Vector3 color;
    }

    [VFXInfo(category = "Sampling")]
    class SampleBuffer : VFXOperator
    {
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
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(DummyTest), "s"));
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var outputSlot = GetOutputSlot(0);
            var slots = outputSlot.GetVFXValueTypeSlots();

            var type = typeof(DummyTest);
            var expressions = new List<VFXExpression>();
            foreach (var slot in slots)
            {
                var current = new VFXExpressionSampleBuffer(type, slot.valueType, slot.name, inputExpression[0], inputExpression[1]);
                expressions.Add(current);
            }
            return expressions.ToArray();
        }
    }
}
