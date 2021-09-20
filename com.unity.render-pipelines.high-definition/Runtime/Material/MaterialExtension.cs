using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDRenderQueue;
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

    /// <summary>Emissive Intensity Unit</summary>
    public enum EmissiveIntensityUnit
    {
        /// <summary>Nits</summary>
        Nits,
        /// <summary>EV100</summary>
        EV100,
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

        public static void UpdateEmissiveColorLDRFromIntensityAndEmissiveColor(this Material material)
        {
            const string kEmissiveColorLDR = "_EmissiveColorLDR";
            const string kEmissiveColor = "_EmissiveColor";
            const string kEmissiveIntensity = "_EmissiveIntensity";

            if (material.HasProperty(kEmissiveColorLDR) && material.HasProperty(kEmissiveIntensity) && material.HasProperty(kEmissiveColor))
            {
                Color emissiveColorLDRLinear = material.GetColor(kEmissiveColor) / material.GetFloat(kEmissiveIntensity);
                Color emissiveColorLDR = new Color(Mathf.LinearToGammaSpace(emissiveColorLDRLinear.r), Mathf.LinearToGammaSpace(emissiveColorLDRLinear.g), Mathf.LinearToGammaSpace(emissiveColorLDRLinear.b));
                material.SetColor(kEmissiveColorLDR, emissiveColorLDR);
            }
        }

        public static DisplacementMode GetFilteredDisplacementMode(this Material material, DisplacementMode displacementMode)
        {
            if (material.HasProperty(kTessellationMode))
            {
                if (displacementMode == DisplacementMode.Pixel || displacementMode == DisplacementMode.Vertex)
                    return DisplacementMode.None;
            }
            else
            {
                if (displacementMode == DisplacementMode.Tessellation)
                    return DisplacementMode.None;
            }
            return displacementMode;
        }
    }
}

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Utility class for setting properties, keywords and passes on a material to ensure it is in a valid state for rendering with HDRP.
    /// </summary>
    public static partial class HDMaterial
    {
        //enum representing all shader and shadergraph that we expose to user
        internal enum ShaderID
        {
            Lit,
            LitTesselation,
            LayeredLit,
            LayeredLitTesselation,
            Unlit,
            Decal,
            TerrainLit,
            AxF,
            Count_Standard,
            SG_Unlit = Count_Standard,
            SG_Lit,
            SG_Hair,
            SG_Fabric,
            SG_StackLit,
            SG_Decal,
            SG_Eye,
            Count_All,
            Count_ShaderGraph = Count_All - Count_Standard,
            SG_External = -1, // material packaged outside of HDRP
        }

        // exposed shader, for reference while searching the ShaderID
        internal static readonly string[] s_ShaderPaths =
        {
            "HDRP/Lit",
            "HDRP/LitTessellation",
            "HDRP/LayeredLit",
            "HDRP/LayeredLitTessellation",
            "HDRP/Unlit",
            "HDRP/Decal",
            "HDRP/TerrainLit",
            "HDRP/AxF",
        };

        // list of methods for resetting keywords
        internal delegate void MaterialResetter(Material material);
        internal static Dictionary<ShaderID, MaterialResetter> k_PlainShadersMaterialResetters = new Dictionary<ShaderID, MaterialResetter>()
        {
            { ShaderID.Lit, LitAPI.ValidateMaterial },
            { ShaderID.LitTesselation, LitAPI.ValidateMaterial },
            { ShaderID.LayeredLit,  LayeredLitAPI.ValidateMaterial },
            { ShaderID.LayeredLitTesselation, LayeredLitAPI.ValidateMaterial },
            { ShaderID.Unlit, UnlitAPI.ValidateMaterial },
            { ShaderID.Decal, DecalAPI.ValidateMaterial },
            { ShaderID.TerrainLit, TerrainLitAPI.ValidateMaterial },
            { ShaderID.AxF, AxFAPI.ValidateMaterial },

            { ShaderID.SG_Unlit, ShaderGraphAPI.ValidateUnlitMaterial },
            { ShaderID.SG_Lit, ShaderGraphAPI.ValidateLightingMaterial },
            { ShaderID.SG_Hair, ShaderGraphAPI.ValidateLightingMaterial },
            { ShaderID.SG_Fabric, ShaderGraphAPI.ValidateLightingMaterial },
            { ShaderID.SG_StackLit, ShaderGraphAPI.ValidateLightingMaterial },
            { ShaderID.SG_Decal, ShaderGraphAPI.ValidateDecalMaterial },
            { ShaderID.SG_Eye, ShaderGraphAPI.ValidateLightingMaterial }
        };

        /// <summary>
        /// Setup properties, keywords and passes on a material to ensure it is in a valid state for rendering with HDRP. This function is only for materials using HDRP Shaders.
        /// </summary>
        /// <param name="material">The target material.</param>
        /// <returns>False if the material doesn't have an HDRP Shader.</returns>
        public static bool ValidateMaterial(Material material)
        {
            var shaderName = material.shader.name;
            var index = Array.FindIndex(s_ShaderPaths, m => m == shaderName);
            if (index == -1)
                return false;

            k_PlainShadersMaterialResetters.TryGetValue((ShaderID)index, out var resetter);
            if (resetter == null)
                return false;

            material.shaderKeywords = null;
            resetter(material);
            return true;
        }
    }

    public static partial class HDMaterial
    {
        /// <summary>Rendering Pass</summary>
        public enum RenderingPass
        {
            /// <summary>Before Refraction. Only for transparent materials</summary>
            BeforeRefraction,
            /// <summary>Default</summary>
            Default,
            /// <summary>After Post Process</summary>
            AfterPostProcess,
            /// <summary>Low Resolution. Only for transparent materials</summary>
            LowResolution,
        }

        static RenderQueueType RenderingPassToQueue(RenderingPass pass, bool isTransparent)
        {
            switch (pass)
            {
                case RenderingPass.Default:
                    return isTransparent ? RenderQueueType.Transparent : RenderQueueType.Opaque;
                case RenderingPass.AfterPostProcess:
                    return isTransparent ? RenderQueueType.AfterPostprocessTransparent : RenderQueueType.AfterPostProcessOpaque;

                case RenderingPass.BeforeRefraction:
                    return isTransparent ? RenderQueueType.PreRefraction : RenderQueueType.Opaque;
                case RenderingPass.LowResolution:
                    return isTransparent ? RenderQueueType.LowTransparent : RenderQueueType.Opaque;

                default:
                    return RenderQueueType.Unknown;
            }
        }

        /// <summary>Set the Rendering Pass</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="value">The rendering pass to set.</param>
        public static void SetRenderingPass(this Material material, RenderingPass value)
        {
            bool isTransparent = (SurfaceType)material.GetFloat(kSurfaceType) == SurfaceType.Transparent;
            var type = RenderingPassToQueue(value, isTransparent);

            int sortingPriority = material.HasProperty(kTransparentSortPriority) ? (int)material.GetFloat(kTransparentSortPriority) : 0;
            bool alphaClipping = material.HasProperty(kAlphaCutoffEnabled) && material.GetFloat(kAlphaCutoffEnabled) > 0.0f;
            bool receiveDecals = material.HasProperty(kEnableDecals) && material.GetFloat(kEnableDecals) > 0.0f;
            material.renderQueue = ChangeType(type, sortingPriority, alphaClipping, receiveDecals);
        }

        /// <summary>Set the Emissive Color</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="value">The emissive color.</param>
        public static void SetEmissiveColor(this Material material, Color value)
        {
            if (material.GetFloat(kUseEmissiveIntensity) > 0.0f)
            {
                material.SetColor(kEmissiveColorLDR, value);
                material.SetColor(kEmissiveColor, value.linear * material.GetFloat(kEmissiveIntensity));
            }
            else
            {
                if (material.HasProperty(kEmissiveColorHDR))
                    material.SetColor(kEmissiveColorHDR, value);
                material.SetColor(kEmissiveColor, value);
            }
        }

        /// <summary>Set the Emissive Intensity</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="intensity">The emissive intensity.</param>
        /// <param name="unit">The unit of the intensity parameter.</param>
        public static void SetEmissiveIntensity(this Material material, float intensity, EmissiveIntensityUnit unit)
        {
            if (unit == EmissiveIntensityUnit.EV100)
                intensity = LightUtils.ConvertEvToLuminance(intensity);
            material.SetFloat(kEmissiveIntensity, intensity);
            material.SetFloat(kEmissiveIntensityUnit, (float)unit);
            material.SetColor(kEmissiveColor, material.GetColor(kEmissiveColorLDR).linear * intensity);
        }

        /// <summary>Set Alpha Clipping</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="value">True to enable alpha clipping.</param>
        public static void SetAlphaClipping(this Material material, bool value)
        {
            material.SetFloat(kAlphaCutoffEnabled, value ? 1.0f : 0.0f);
            material.SetupBaseUnlitKeywords();
        }

        /// <summary>Set Alpha Cutoff</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="cutoff">The alpha cutoff value between 0 and 1.</param>
        public static void SetAlphaCutoff(this Material material, float cutoff)
        {
            material.SetFloat(kAlphaCutoff, cutoff);
            material.SetFloat("_Cutoff", cutoff);
        }

        /// <summary>Set the Diffusion profile on a standard HDRP shader.</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="profile">The Diffusion Profile Asset.</param>
        public static void SetDiffusionProfile(this Material material, DiffusionProfileSettings profile)
        {
            float hash = profile != null ? HDShadowUtils.Asfloat(profile.profile.hash) : 0;
            material.SetFloat(HDShaderIDs._DiffusionProfileHash, hash);

#if UNITY_EDITOR
            material.SetDiffusionProfileAsset(profile, HDShaderIDs._DiffusionProfileAsset);
#endif
        }

        /// <summary>Set the Diffusion profile on a Shader Graph material.</summary>
        /// <param name="material">The material to change.</param>
        /// <param name="profile">The Diffusion Profile Asset.</param>
        /// <param name="referenceName">The reference name of the Diffusion Profile property in the Shader Graph.</param>
        public static void SetDiffusionProfileShaderGraph(this Material material, DiffusionProfileSettings profile, string referenceName)
        {
            float hash = profile != null ? HDShadowUtils.Asfloat(profile.profile.hash) : 0;
            material.SetFloat(referenceName, hash);

#if UNITY_EDITOR
            material.SetDiffusionProfileAsset(profile, Shader.PropertyToID(referenceName + "_Asset"));
#endif
        }

#if UNITY_EDITOR
        internal static void SetDiffusionProfileAsset(this Material material, DiffusionProfileSettings profile, int assetPropertyId, int index = 0)
        {
            Vector4 guid = Vector3.zero;
            if (profile != null)
                guid = HDUtils.ConvertGUIDToVector4(UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(profile)));
            material.SetVector(assetPropertyId, guid);

            var externalRefs = MaterialExternalReferences.GetMaterialExternalReferences(material);
            externalRefs.SetDiffusionProfileReference(index, profile);
        }
#endif

        // this will work on ALL shadergraph-built shaders, in memory or asset based
        internal static bool IsShaderGraph(Material material)
        {
            var shaderGraphTag = material.GetTag("ShaderGraphShader", false, null);
            return !string.IsNullOrEmpty(shaderGraphTag);
        }
    }
}
