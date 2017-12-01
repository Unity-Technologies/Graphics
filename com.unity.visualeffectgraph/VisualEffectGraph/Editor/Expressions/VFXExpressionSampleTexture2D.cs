using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleTexture2D : VFXExpression
    {
        public VFXExpressionSampleTexture2D() : this(VFXValue<Texture2D>.Default, VFXValue<Vector2>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleTexture2D(VFXExpression texture, VFXExpression uv, VFXExpression mipLevel)
            : base(Flags.InvalidOnCPU, new VFXExpression[3] { texture, uv, mipLevel })
        {}

        sealed public override VFXExpressionOp operation { get { return VFXExpressionOp.kVFXSampleTexture2D; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleTexture({0},{1},{2})", parents[0], parents[1], parents[2]);
        }
    }
}
