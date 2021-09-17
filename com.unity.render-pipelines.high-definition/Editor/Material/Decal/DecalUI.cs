using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Decal materials (does not include ShaderGraphs)
    /// </summary>
    class DecalUI : HDShaderGUI
    {
        [Flags]
        enum Expandable : uint
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
            Sorting = 1 << 2,
        }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new DecalSurfaceOptionsUIBlock((MaterialUIBlock.ExpandableBit)Expandable.SurfaceOptions),
            new DecalSurfaceInputsUIBlock((MaterialUIBlock.ExpandableBit)Expandable.SurfaceInputs),
            new DecalSortingInputsUIBlock((MaterialUIBlock.ExpandableBit)Expandable.Sorting),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            uiBlocks.OnGUI(materialEditor, props);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupCommonDecalMaterialKeywordsAndPass(Material material)
        {
            bool affectsMaskmap = false;
            affectsMaskmap |= material.HasProperty(kAffectMetal) && material.GetFloat(kAffectMetal) == 1.0f;
            affectsMaskmap |= material.HasProperty(kAffectAO) && material.GetFloat(kAffectAO) == 1.0f;
            affectsMaskmap |= material.HasProperty(kAffectSmoothness) && material.GetFloat(kAffectSmoothness) == 1.0f;

            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_ALBEDO", material.HasProperty(kAffectAlbedo) && material.GetFloat(kAffectAlbedo) == 1.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_NORMAL", material.HasProperty(kAffectNormal) && material.GetFloat(kAffectNormal) == 1.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_MASKMAP", affectsMaskmap);

            // Albedo : RT0 RGB, A - sRGB
            // Normal : RT1 RGB, A
            // Smoothness: RT2 B, A
            // Metal: RT2 R, RT3 R
            // AO: RT2 G, RT3 G
            // Note RT3 is only RG
            ColorWriteMask mask0 = 0, mask1 = 0, mask2 = 0, mask3 = 0;

            if (material.HasProperty(kAffectAlbedo) && material.GetFloat(kAffectAlbedo) == 1.0f)
                mask0 |= ColorWriteMask.All;
            if (material.HasProperty(kAffectNormal) && material.GetFloat(kAffectNormal) == 1.0f)
                mask1 |= ColorWriteMask.All;
            if (material.HasProperty(kAffectMetal) && material.GetFloat(kAffectMetal) == 1.0f)
                mask2 |= mask3 |= ColorWriteMask.Red;
            if (material.HasProperty(kAffectAO) && material.GetFloat(kAffectAO) == 1.0f)
                mask2 |= mask3 |= ColorWriteMask.Green;
            if (material.HasProperty(kAffectSmoothness) && material.GetFloat(kAffectSmoothness) == 1.0f)
                mask2 |= ColorWriteMask.Blue | ColorWriteMask.Alpha;

            material.SetInt(HDShaderIDs._DecalColorMask0, (int)mask0);
            material.SetInt(HDShaderIDs._DecalColorMask1, (int)mask1);
            material.SetInt(HDShaderIDs._DecalColorMask2, (int)mask2);
            material.SetInt(HDShaderIDs._DecalColorMask3, (int)mask3);

            // First reset the pass (in case new shader graph add or remove a pass)
            material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferProjectorStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferMeshStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DecalMeshForwardEmissiveStr, true);

            // Then disable pass is they aren't needed
            if (material.FindPass(HDShaderPassNames.s_DBufferProjectorStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferProjectorStr, ((int)mask0 + (int)mask1 + (int)mask2 + (int)mask3) != 0);
            if (material.FindPass(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr, material.HasProperty(kAffectEmission) && material.GetFloat(kAffectEmission) == 1.0f);
            if (material.FindPass(HDShaderPassNames.s_DBufferMeshStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferMeshStr, ((int)mask0 + (int)mask1 + (int)mask2 + (int)mask3) != 0);
            if (material.FindPass(HDShaderPassNames.s_DecalMeshForwardEmissiveStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DecalMeshForwardEmissiveStr, material.HasProperty(kAffectEmission) && material.GetFloat(kAffectEmission) == 1.0f);

            // Set stencil state
            material.SetInt(kDecalStencilWriteMask, (int)StencilUsage.Decals);
            material.SetInt(kDecalStencilRef, (int)StencilUsage.Decals);

            // Set render queue
            var renderQueue = -1;
            if (material.HasProperty(HDShaderIDs._DrawOrder))
                renderQueue = (int)RenderQueue.Geometry + material.GetInt(HDShaderIDs._DrawOrder);
            material.renderQueue = renderQueue;

            // always instanced
            material.enableInstancing = true;
        }

        protected const string kBaseColorMap = "_BaseColorMap";
        protected const string kMaskMap = "_MaskMap";
        protected const string kNormalMap = "_NormalMap";
        protected const string kEmissiveColorMap = "_EmissiveColorMap";

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupDecalKeywordsAndPass(Material material)
        {
            // Setup color mask properties
            SetupCommonDecalMaterialKeywordsAndPass(material);

            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            CoreUtils.SetKeyword(material, "_EMISSIVEMAP", material.GetTexture(kEmissiveColorMap));
        }

        public override void ValidateMaterial(Material material) => SetupDecalKeywordsAndPass(material);
    }
}
