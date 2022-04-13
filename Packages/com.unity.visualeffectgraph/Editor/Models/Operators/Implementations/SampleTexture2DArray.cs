using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling")]
    class SampleTexture2DArray : VFXOperator
    {
        override public string name { get { return "Sample Texture2DArray"; } }

        public class InputProperties
        {
            [Tooltip("Sets the texture to sample from.")]
            public Texture2DArray texture = null;
            [Tooltip("Sets the texture coordinate used for the sampling.")]
            public Vector2 UV = Vector2.zero;
            [Min(0), Tooltip("Sets the array slice to sample from.")]
            public float slice = 0.0f;
            [Min(0), Tooltip("Sets the mip level to sample from.")]
            public float mipLevel = 0.0f;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the sampled value from the texture's specified slice at the specified UV coordinate.")]
            public Vector4 s = Vector4.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleTexture2DArray(inputExpression[0], inputExpression[1], inputExpression[2], inputExpression[3]) };
        }
    }
}
