using UnityEngine;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Lit shader graphs
    /// </summary>
    class HDLitGUI : HDShaderGUI
    {
        // For surface option shader graph we only want all unlit features but alpha clip and back then front rendering
        const SurfaceOptionUIBlock.Features   surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit
            ^ SurfaceOptionUIBlock.Features.AlphaCutoffThreshold
            ^ SurfaceOptionUIBlock.Features.BackThenFrontRendering
            ^ SurfaceOptionUIBlock.Features.ShowAfterPostProcessPass
            ^ SurfaceOptionUIBlock.Features.ReceiveSSR;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: surfaceOptionFeatures),
            new ShaderGraphUIBlock(MaterialUIBlock.Expandable.ShaderGraph),
        };

        protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);
                ApplyKeywordsAndPassesIfNeeded(changed.changed, uiBlocks.materials);
            }
        }

        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            SynchronizeShaderGraphProperties(material);

            BaseLitGUI.SetupBaseLitKeywords(material);
            BaseLitGUI.SetupBaseLitMaterialPass(material);
            bool receiveSSR = material.HasProperty(kReceivesSSR) ? material.GetInt(kReceivesSSR) != 0 : false;
            bool useSplitLighting = material.HasProperty(kUseSplitLighting) ? material.GetInt(kUseSplitLighting) != 0: false;
            bool isLitSSS = material.GetMaterialId() == MaterialId.LitSSS;

            BaseLitGUI.SetupStencil(material, receiveSSR, useSplitLighting || isLitSSS);
            if (material.HasProperty(kAddPrecomputedVelocity))
            {
                CoreUtils.SetKeyword(material, "_ADD_PRECOMPUTED_VELOCITY", material.GetInt(kAddPrecomputedVelocity) != 0);
            }

            if (material.HasProperty(kMaterialID))
            {
                MaterialId materialId = material.GetMaterialId();
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING", materialId == MaterialId.LitSSS);
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_TRANSMISSION", materialId == MaterialId.LitTranslucent
                                    || (materialId == MaterialId.LitSSS && material.GetFloat(kTransmissionEnable) > 0.0f));

                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_ANISOTROPY", materialId == MaterialId.LitAniso);

                // No material Id for clear coat, just test the attribute
                var coatMaskMap = material.HasProperty("_CoatMaskMap");
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_CLEAR_COAT",
                                     material.GetFloat("_CoatMask") > 0.0 || (coatMaskMap && material.GetTexture("_CoatMaskMap")));

                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_IRIDESCENCE", materialId == MaterialId.LitIridescence);
                CoreUtils.SetKeyword(material, "_MATERIAL_FEATURE_SPECULAR_COLOR", materialId == MaterialId.LitSpecular);
            }
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
