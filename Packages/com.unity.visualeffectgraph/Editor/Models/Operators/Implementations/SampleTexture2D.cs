using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SampleTexture2D")]
    [VFXInfo(category = "Sampling")]
    class SampleTexture2D : VFXOperator
    {
        override public string name { get { return "Sample Texture2D"; } }

        public class InputProperties
        {
            [Tooltip("Sets the texture to sample from.")]
            public Texture2D texture = null;
            [Tooltip("Sets the texture coordinate used for the sampling.")]
            public Vector2 UV = Vector2.zero;
            [Min(0), Tooltip("Sets the mip level to sample from.")]
            public float mipLevel = 0.0f;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the sampled value from the texture at the specified UV coordinate.")]
            public Vector4 s = Vector4.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleTexture2D(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
