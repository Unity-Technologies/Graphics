using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    sealed class VFXExpressionSampleCameraBuffer : VFXExpression
    {
        public VFXExpressionSampleCameraBuffer() : this(VFXCameraBufferValue.Default, VFXValue<Vector2>.Default)
        {
        }

        public VFXExpressionSampleCameraBuffer(VFXExpression cameraBuffer, VFXExpression uv)
            : base(Flags.InvalidOnCPU, new [] { cameraBuffer, uv })
        {
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.None;
        public override VFXValueType valueType => VFXValueType.Float4;

        public override string GetCodeString(string[] parents)
        {
            return string.Format("SAMPLE_TEXTURE2D_X_LOD({0}, sampler{0}, {1}, 0)", parents[0], parents[1]);
        }
    }
}
