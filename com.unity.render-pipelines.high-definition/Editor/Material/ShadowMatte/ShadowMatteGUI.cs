using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP unlit shaders (does not include shader graphs)
    /// </summary>
    class ShadowMatteGUI : HDShaderGUI
    {
        const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit
            ^ SurfaceOptionUIBlock.Features.AlphaCutoff
            ^ SurfaceOptionUIBlock.Features.AlphaCutoffThreshold
            ^ SurfaceOptionUIBlock.Features.AlphaCutoffShadowThreshold;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: surfaceOptionFeatures),
            new ShadowMatteUIBlock(MaterialUIBlock.Expandable.Input),
            new UnlitSurfaceInputsUIBlock(MaterialUIBlock.Expandable.Input),
            new TransparencyUIBlock(MaterialUIBlock.Expandable.Transparency),
            new AdvancedOptionsUIBlock(MaterialUIBlock.Expandable.Advance, AdvancedOptionsUIBlock.Features.Instancing | AdvancedOptionsUIBlock.Features.AddPrecomputedVelocity)
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);

                // Apply material keywords and pass:
                if (changed.changed)
                {
                    foreach (var material in uiBlocks.materials)
                        SetupMaterialKeywordsAndPassInternal(material);
                }
            }
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupShadowMatteMaterialKeywordsAndPass(material);

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        public static void SetupShadowMatteMaterialKeywordsAndPass(Material material)
        {
            UnlitGUI.SetupUnlitMaterialKeywordsAndPass(material);

            material.SetupBaseUnlitKeywords();
            material.SetupBaseUnlitPass();

            var mainTex = material.GetTexture(ShadowMatteUIBlock.kColorMap);
            material.SetTexture("_ShadowTintMap", mainTex);
            Color color = material.GetColor(ShadowMatteUIBlock.kColor);
            material.SetColor("_ShadowTint", color);
            int shadowFilterPoint  = material.GetInt(ShadowMatteUIBlock.kShadowFilterPoint);
            int shadowFilterDir    = material.GetInt(ShadowMatteUIBlock.kShadowFilterDir);
            int shadowFilterRect   = material.GetInt(ShadowMatteUIBlock.kShadowFilterArea);
            uint finalFlag = 0x00000000;
            if (shadowFilterPoint == 1)
                finalFlag |= unchecked((uint)LightFeatureFlags.Punctual);
            if (shadowFilterDir == 1)
                finalFlag |= unchecked((uint)LightFeatureFlags.Directional);
            if (shadowFilterRect == 1)
                finalFlag |= unchecked((uint)LightFeatureFlags.Area);
            material.SetInt("_ShadowFilter", unchecked((int)finalFlag));
        }
    }
} // namespace UnityEditor
