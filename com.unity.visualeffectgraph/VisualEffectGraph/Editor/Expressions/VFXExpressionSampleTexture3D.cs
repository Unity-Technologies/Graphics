using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleTexture3D : VFXExpression
    {
        public VFXExpressionSampleTexture3D() : this(VFXValue<Texture3D>.Default, VFXValue<Vector3>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleTexture3D(VFXExpression texture, VFXExpression uv, VFXExpression mipLevel)
            : base(Flags.InvalidOnCPU, new VFXExpression[3] { texture, uv, mipLevel })
        {}

        sealed public override VFXExpressionOp operation { get { return VFXExpressionOp.kVFXSampleTexture3D; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("SampleTexture(VFX_SAMPLER({0}),{1},{2})", parents[0], parents[1], parents[2]);
        }
    }
}
