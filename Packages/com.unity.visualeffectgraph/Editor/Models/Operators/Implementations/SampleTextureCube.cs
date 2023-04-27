using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SampleTextureCube")]
    [VFXInfo(category = "Sampling")]
    class SampleTextureCube : VFXOperator
    {
        override public string name { get { return "Sample TextureCube"; } }

        public class InputProperties
        {
            [Tooltip("Sets the texture to sample from.")]
            public Cubemap texture = null;
            [Tooltip("Sets the texture coordinate used for the sampling.")]
            public Vector3 UVW = Vector3.zero;
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
            return new[] { new VFXExpressionSampleTextureCube(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
