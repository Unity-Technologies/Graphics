using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionLoadTexture2D : VFXExpression
    {
        public VFXExpressionLoadTexture2D() : this(VFXTexture2DValue.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionLoadTexture2D(VFXExpression texture, VFXExpression location)
            : base(Flags.InvalidOnCPU, new VFXExpression[2] { texture, location})
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.None; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Float4; } }

        public sealed override string GetCodeString(string[] parents)
        {
            return string.Format("LoadTexture(VFX_SAMPLER({0}),(int3){1})", parents[0], parents[1]);
        }
    }
}
