using UnityEditor.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class DecalAPI
    {
        internal static void SetupCommonDecalMaterialKeywordsAndPass(Material material)
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

            bool enableDBufferPass = ((int)mask0 + (int)mask1 + (int)mask2 + (int)mask3) != 0;
            bool enableForwardEmissivePass = material.HasProperty(kAffectEmission) && material.GetFloat(kAffectEmission) == 1.0f;

            // Disable the passes that aren't needed
            // We have to check that the passes can be found on the material before touching them
            // This is important because in some cases, HDRP validation code may be run before HDRP initialization
            // This happens when importing with clean Library a project that needs to run the material upgrader
            // Consequence is that Shader.globalRenderPipeline is not set so FindPadd will ignore passes marked with HDRP tag
            // So in this code, we have to assume that even if we don't find a pass, it may still exist
            
            if (material.FindPass(HDShaderPassNames.s_DBufferMeshStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferMeshStr, enableDBufferPass);
            if (material.FindPass(HDShaderPassNames.s_DBufferProjectorStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferProjectorStr, enableDBufferPass);

            if (material.FindPass(HDShaderPassNames.s_DecalMeshForwardEmissiveStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DecalMeshForwardEmissiveStr, enableForwardEmissivePass);
            if (material.FindPass(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr, enableForwardEmissivePass);

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

        // All Validate functions must be static. It allows to automatically update the shaders with a script if code changes
        internal static void ValidateMaterial(Material material)
        {
            // Setup color mask properties
            SetupCommonDecalMaterialKeywordsAndPass(material);

            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            CoreUtils.SetKeyword(material, "_EMISSIVEMAP", material.GetTexture(kEmissiveColorMap));

            if (material.GetFloat(kUseEmissiveIntensity) == 0)
                material.SetColor(kEmissiveColor, material.GetColor(kEmissiveColorHDR));
            else
                material.UpdateEmissiveColorFromIntensityAndEmissiveColorLDR();
        }
    }
}
