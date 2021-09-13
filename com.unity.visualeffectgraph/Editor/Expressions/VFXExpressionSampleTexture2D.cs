using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleTexture2D : VFXExpression
    {
        public VFXExpressionSampleTexture2D() : this(VFXTexture2DValue.Default, VFXValue<Vector2>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleTexture2D(VFXExpression texture, VFXExpression uv, VFXExpression mipLevel)
            : base(Flags.InvalidOnCPU, new VFXExpression[3] { texture, uv, mipLevel })
        { }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleTexture(VFX_SAMPLER({0}),{1},{2})", parents[0], parents[1], parents[2]);
        }
    }
}
