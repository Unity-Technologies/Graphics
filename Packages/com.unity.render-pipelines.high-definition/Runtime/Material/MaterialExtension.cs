using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    // Note: There is another SurfaceType in ShaderGraph (AlphaMode.cs) which conflicts in HDRP shader graph files
    enum SurfaceType
    {
        Opaque,
        Transparent
    }

    enum DisplacementMode
    {
        None,
        Vertex,
        Pixel,
        Tessellation
    }

    enum DoubleSidedNormalMode
    {
        Flip,
        Mirror,
        None
    }

    enum DoubleSidedGIMode
    {
        Auto,
        On,
        Off
    }

    enum TessellationMode
    {
        None,
        Phong
    }

    enum MaterialId
    {
        LitSSS = 0,
        LitStandard = 1,
        LitAniso = 2,
        LitIridescence = 3,
        LitSpecular = 4,
        LitTranslucent = 5
    };

    enum NormalMapSpace
    {
        TangentSpace,
        ObjectSpace,
    }

    enum HeightmapMode
    {
        Parallax,
        Displacement,
    }

    enum VertexColorMode
    {
        None,
        Multiply,
        Add
    }

    internal enum UVDetailMapping
    {
        UV0,
        UV1,
        UV2,
        UV3
    }

    internal enum UVBaseMapping
    {
        UV0,
        UV1,
        UV2,
        UV3,
        Planar,
        Triplanar
    }

    internal enum UVEmissiveMapping
    {
        UV0,
        UV1,
        UV2,
        UV3,
        Planar,
        Triplanar,
        SameAsBase
    }

    internal enum HeightmapParametrization
    {
        MinMax = 0,
        Amplitude = 1
    }

    internal enum TransparentCullMode
    {
        // Off is double sided and a different setting so we don't have it here
        Back = CullMode.Back,
        Front = CullMode.Front,
    }

    internal enum OpaqueCullMode
    {
        // Off is double sided and a different setting so we don't have it here
        Back = CullMode.Back,
        Front = CullMode.Front,
    }

    internal static class MaterialExtension
    {
        public static SurfaceType GetSurfaceType(this Material material)
            => material.HasProperty(kSurfaceType) ? (SurfaceType)material.GetFloat(kSurfaceType) : SurfaceType.Opaque;

        public static MaterialId GetMaterialId(this Material material)
            => material.HasProperty(kMaterialID) ? (MaterialId)material.GetFloat(kMaterialID) : MaterialId.LitStandard;

        public static BlendMode GetBlendMode(this Material material)
            => material.HasProperty(kBlendMode) ? (BlendMode)material.GetFloat(kBlendMode) : BlendMode.Additive;

        public static int GetLayerCount(this Material material)
            => material.HasProperty(kLayerCount) ? material.GetInt(kLayerCount) : 1;

        public static bool GetZWrite(this Material material)
            => material.HasProperty(kZWrite) ? material.GetInt(kZWrite) == 1 : false;

        public static bool GetTransparentZWrite(this Material material)
            => material.HasProperty(kTransparentZWrite) ? material.GetInt(kTransparentZWrite) == 1 : false;

        public static CullMode GetTransparentCullMode(this Material material)
            => material.HasProperty(kTransparentCullMode) ? (CullMode)material.GetInt(kTransparentCullMode) : CullMode.Back;

        public static CullMode GetOpaqueCullMode(this Material material)
            => material.HasProperty(kOpaqueCullMode) ? (CullMode)material.GetInt(kOpaqueCullMode) : CullMode.Back;

        public static CompareFunction GetTransparentZTest(this Material material)
            => material.HasProperty(kZTestTransparent) ? (CompareFunction)material.GetInt(kZTestTransparent) : CompareFunction.LessEqual;

        public static void ResetMaterialCustomRenderQueue(this Material material)
        {
            // using GetOpaqueEquivalent / GetTransparentEquivalent allow to handle the case when we switch surfaceType
            HDRenderQueue.RenderQueueType targetQueueType;
            switch (material.GetSurfaceType())
            {
                case SurfaceType.Opaque:
                    targetQueueType = HDRenderQueue.GetOpaqueEquivalent(HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue));
                    break;
                case SurfaceType.Transparent:
                    targetQueueType = HDRenderQueue.GetTransparentEquivalent(HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue));
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }

            float sortingPriority = material.HasProperty(kTransparentSortPriority) ? material.GetFloat(kTransparentSortPriority) : 0.0f;
            bool alphaTest = material.HasProperty(kAlphaCutoffEnabled) && material.GetFloat(kAlphaCutoffEnabled) > 0.0f;
            bool decalEnable = material.HasProperty(kEnableDecals) && material.GetFloat(kEnableDecals) > 0.0f;
            material.renderQueue = HDRenderQueue.ChangeType(targetQueueType, (int)sortingPriority, alphaTest, decalEnable);
        }

        public static void UpdateEmissiveColorFromIntensityAndEmissiveColorLDR(this Material material)
        {
            const string kEmissiveColorLDR = "_EmissiveColorLDR";
            const string kEmissiveColor = "_EmissiveColor";
            const string kEmissiveIntensity = "_EmissiveIntensity";

            if (material.HasProperty(kEmissiveColorLDR) && material.HasProperty(kEmissiveIntensity) && material.HasProperty(kEmissiveColor))
            {
                // Important: The color picker for kEmissiveColorLDR is LDR and in sRGB color space but Unity don't perform any color space conversion in the color
                // picker BUT only when sending the color data to the shader... So as we are doing our own calculation here in C#, we must do the conversion ourselves.
                Color emissiveColorLDR = material.GetColor(kEmissiveColorLDR);
                Color emissiveColorLDRLinear = new Color(Mathf.GammaToLinearSpace(emissiveColorLDR.r), Mathf.GammaToLinearSpace(emissiveColorLDR.g), Mathf.GammaToLinearSpace(emissiveColorLDR.b));
                material.SetColor(kEmissiveColor, emissiveColorLDRLinear * material.GetFloat(kEmissiveIntensity));
            }
        }
    }
}
