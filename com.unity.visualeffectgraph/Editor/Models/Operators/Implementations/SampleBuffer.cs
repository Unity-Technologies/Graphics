using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    struct DummyTest
    {
        Vector3 position;
        Vector3 color;
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
            public UInt32 index;
        }

        public class OutputProperties
        {
            [Tooltip("TODO. //Will be dynamic")]
            public Vector3 position;

            [Tooltip("TODO. //Will be dynamic")]
            public Vector3 color;
        }


        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var samplePosition = new VFXExpressionSampleBuffer(typeof(DummyTest), "position", inputExpression[0], inputExpression[1]);
            var samplecolor = new VFXExpressionSampleBuffer(typeof(DummyTest), "color", inputExpression[0], inputExpression[1]);
            return new[] { samplePosition, samplecolor };
        }
    }
}
