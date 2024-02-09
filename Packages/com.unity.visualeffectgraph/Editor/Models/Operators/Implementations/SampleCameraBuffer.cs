using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SampleCameraBuffer")]
    [VFXInfo(category = "Sampling")]
    sealed class SampleCameraBuffer : VFXOperator
    {
        public override string name { get { return "Sample Camera Buffer"; } }

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

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression textureWidth = new VFXExpressionTextureWidth(inputExpression[0]);
            VFXExpression textureHeight = new VFXExpressionTextureHeight(inputExpression[0]);
            textureWidth = new VFXExpressionCastUintToFloat(textureWidth);
            textureHeight = new VFXExpressionCastUintToFloat(textureHeight);
            VFXExpression texelSize = new VFXExpressionCombine(textureWidth, textureHeight);
            texelSize = VFXOperatorUtility.Reciprocal(texelSize);

            return new [] { new VFXExpressionSampleCameraBuffer(inputExpression[0], inputExpression[1] * inputExpression[2] * texelSize) };
        }
    }
}
