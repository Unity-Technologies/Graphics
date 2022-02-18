using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling")]
    class SampleCameraBuffer : VFXOperator
    {
        override public string name { get { return "Sample Camera Buffer"; } }

        public class InputProperties
        {
            [Tooltip("Sets the camera buffer to sample from.")]
            public CameraBuffer cameraBuffer = null;
            [Tooltip("Sets the camera pixel dimensions.")]
            public Vector2 pixelDimensions = Vector2.one;
            [Tooltip("Sets the texture coordinate used for the sampling.")]
            public Vector2 UV = Vector2.zero;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the sampled value from the camera buffer at the specified UV coordinate.")]
            public Vector4 s = Vector4.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleCameraBuffer(inputExpression[0], inputExpression[1], inputExpression[2]) };
        }
    }
}
