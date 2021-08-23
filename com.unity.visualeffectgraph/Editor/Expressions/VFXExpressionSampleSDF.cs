using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.VFX.Operator;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionSampleSDF : VFXExpression
    {
        public VFXExpressionSampleSDF() : this(VFXTexture3DValue.Default, VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleSDF(VFXExpression texture, VFXExpression uvw, VFXExpression scale, VFXExpression mipLevel)
            : base(Flags.InvalidOnCPU, new VFXExpression[4] { texture, uvw, scale, mipLevel })
        { }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("GetDistanceFromSDF(VFX_SAMPLER({0}), {1}, {2}, {3}) ", parents[0], parents[1], parents[2], parents[3]);
        }
    }


    class VFXExpressionSampleSDFNormal : VFXExpression
    {
        public VFXExpressionSampleSDFNormal() : this(VFXTexture3DValue.Default, VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionSampleSDFNormal(VFXExpression texture, VFXExpression inverseTRS, VFXExpression uvw, VFXExpression mipLevel)
            : base(Flags.InvalidOnCPU, new VFXExpression[4] { texture, inverseTRS, uvw, mipLevel })

        { }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float3; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("VFXSafeNormalize(mul(float4(GetNormalFromSDF(VFX_SAMPLER({0}), {2}, {3}),0.0f), {1}).xyz )", parents[0], parents[1], parents[2], parents[3]);
        }
    }
}
