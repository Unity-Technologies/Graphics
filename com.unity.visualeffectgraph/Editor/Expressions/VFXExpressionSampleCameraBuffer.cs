using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleCameraBuffer : VFXExpression
    {
        public VFXExpressionSampleCameraBuffer() : this(VFXCameraBufferValue.Default, VFXValue<Vector2>.Default, VFXValue<Vector2>.Default)
        {
        }

        public VFXExpressionSampleCameraBuffer(VFXExpression cameraBuffer, VFXExpression pixelDimensions, VFXExpression uv)
            : base(Flags.InvalidOnCPU, new VFXExpression[3] { cameraBuffer, pixelDimensions, uv })
        {
        }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SAMPLE_TEXTURE2D_X_LOD({0}, sampler{0}, {2}*{1}*{0}_TexelSize, 0)", parents[0], parents[1], parents[2]);
        }
    }
}
