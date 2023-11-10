using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SampleTextureCubeArray")]
    [VFXInfo(name = "Sample TextureCubeArray", category = "Sampling")]
    class SampleTextureCubeArray : VFXOperator
    {
        override public string name { get { return "Sample TextureCubeArray"; } }

        public class InputProperties
        {
            [Tooltip("Sets the texture to sample from.")]
            public CubemapArray texture = null;
            [Tooltip("Sets the texture coordinate used for the sampling.")]
            public Vector3 UVW = Vector3.zero;
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
            return new[] { new VFXExpressionSampleTextureCubeArray(inputExpression[0], inputExpression[1], inputExpression[2], inputExpression[3]) };
        }
    }
}
