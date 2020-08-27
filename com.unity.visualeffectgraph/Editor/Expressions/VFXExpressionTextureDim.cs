using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionTextureWidth : VFXExpression
    {
        public VFXExpressionTextureWidth() : this(VFXTexture2DValue.Default)
        {}

        public VFXExpressionTextureWidth(VFXExpression texture)
            : base(Flags.InvalidOnGPU, new VFXExpression[1] { texture })
        {}

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.TextureWidth; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Uint32; } }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var tex = constParents[0].Get<Texture>();
            return VFXValue.Constant<uint>(tex ? (uint)tex.width : 0u);
        }
    }

    class VFXExpressionTextureHeight : VFXExpression
    {
        public VFXExpressionTextureHeight() : this(VFXTexture2DValue.Default)
        { }

        public VFXExpressionTextureHeight(VFXExpression texture)
            : base(Flags.InvalidOnGPU, new VFXExpression[1] { texture })
        { }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.TextureHeight; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Uint32; } }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var tex = constParents[0].Get<Texture>();
            return VFXValue.Constant<uint>(tex ? (uint)tex.height : 0u);
        }
    }

    class VFXExpressionTextureDepth : VFXExpression
    {
        public VFXExpressionTextureDepth() : this(VFXTexture2DValue.Default)
        { }

        public VFXExpressionTextureDepth(VFXExpression texture)
            : base(Flags.InvalidOnGPU, new VFXExpression[1] { texture })
        { }

        sealed public override VFXExpressionOperation operation { get { return VFXExpressionOperation.TextureDepth; } }
        sealed public override VFXValueType valueType { get { return VFXValueType.Uint32; } }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var tex = constParents[0].Get<Texture>();
            uint depth = 0u;

            if (tex != null)
            {
                if (tex is Texture3D)
                    depth = (uint)((Texture3D)tex).depth;
                else if (tex is Texture2DArray)
                    depth = (uint)((Texture2DArray)tex).depth;
                else if (tex is CubemapArray)
                    depth = (uint)((CubemapArray)tex).cubemapCount;
                else
                    depth = 1u;
            }

            return VFXValue.Constant<uint>(depth);
        }
    }
}
