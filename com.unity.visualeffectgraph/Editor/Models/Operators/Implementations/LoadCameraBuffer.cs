using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling")]
    class LoadCameraBuffer : VFXOperator
    {
        override public string name { get { return "Load Camera Buffer"; } }

        public class InputProperties
        {
            [Tooltip("Sets the camera buffer to load from.")]
            public CameraBuffer cameraBuffer = null;
            [Tooltip("Sets the x coordinate for the texel to load.")]
            public uint x = 0;
            [Tooltip("Sets the y coordinate for the texel to load.")]
            public uint y = 0;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the sampled value from the camera buffer at the specified coordinate.")]
            public Vector4 s = Vector4.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var location = new VFXExpressionCombine(
                new VFXExpressionCastUintToFloat(inputExpression[1]),
                new VFXExpressionCastUintToFloat(inputExpression[2]));
            return new[] { new VFXExpressionLoadCameraBuffer(inputExpression[0], location) };
        }
    }
}
