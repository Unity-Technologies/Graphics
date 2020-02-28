using UnityEngine;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Lit shader graphs
    /// </summary>
    class SpeedTreeLitGUI : HDShaderGUI
    {
        // For surface option shader graph we only want all unlit features but alpha clip and back then front rendering
        const SurfaceOptionUIBlock.Features   surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Unlit
            ^ SurfaceOptionUIBlock.Features.AlphaCutoff
            ^ SurfaceOptionUIBlock.Features.BackThenFrontRendering
            ^ SurfaceOptionUIBlock.Features.ShowAfterPostProcessPass;

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new SurfaceOptionUIBlock(MaterialUIBlock.Expandable.Base, features: surfaceOptionFeatures),
            new SpeedTreeLitOptionsUIBlock(MaterialUIBlock.Expandable.Other),
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
            bool useSplitLighting = material.HasProperty(kUseSplitLighting) ? material.GetInt(kUseSplitLighting) != 0 : false;
            BaseLitGUI.SetupStencil(material, receiveSSR, useSplitLighting);
            if (material.HasProperty(kAddPrecomputedVelocity))
            {
                CoreUtils.SetKeyword(material, "_ADD_PRECOMPUTED_VELOCITY", material.GetInt(kAddPrecomputedVelocity) != 0);
            }

            // Assume that we are SpeedTree v7 by default.
            int treeVersion = (int)SpeedTreeLitOptionsUIBlock.SpeedTreeVersionEnum.SpeedTreeVer7;
            if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kAssetVersion))
            {
                treeVersion = material.GetInt(SpeedTreeLitOptionsUIBlock.kAssetVersion);
            }
            material.EnableKeyword(HDSpeedTreeTarget.SpeedTreeVersion.ToKeywordString(treeVersion));

            // Only SpeedTree 7 assets should have a GeomType property.
            if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kGeomType) && treeVersion == 0)
            {
                int v = material.GetInt(SpeedTreeLitOptionsUIBlock.kGeomType);
                material.EnableKeyword(HDSpeedTreeTarget.SpeedTree7GeomType.ToKeywordString(v));
            }
            else
            {
                material.EnableKeyword(HDSpeedTreeTarget.SpeedTree7GeomType.ToKeywordString(HDSpeedTreeTarget.kNullGeomType));
            }

            // SpeedTree 7 uses the _WindQuality property directly, but 8 generates a define for it.
            if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kWindEnable))
            {
                bool windOn = material.GetInt(SpeedTreeLitOptionsUIBlock.kWindEnable) != 0;
                CoreUtils.SetKeyword(material, HDSpeedTreeTarget.EnableWind.ToKeywordString(1), windOn);

                if (windOn && material.HasProperty(SpeedTreeLitOptionsUIBlock.kWindQuality) && treeVersion == 1)
                {
                    int quality = material.GetInt(SpeedTreeLitOptionsUIBlock.kWindQuality);
                    material.EnableKeyword(HDSpeedTreeTarget.SpeedTree8WindQuality.ToKeywordString(quality));
                }
                else if (windOn && treeVersion == 0)
                {
                    material.EnableKeyword(HDSpeedTreeTarget.SpeedTree8WindQuality.ToKeywordString(HDSpeedTreeTarget.kNullWindQuality));
                }
            }

            if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kIsBillboard))
            {
                bool billboardOn = material.GetInt(SpeedTreeLitOptionsUIBlock.kIsBillboard) != 0;
                CoreUtils.SetKeyword(material, HDSpeedTreeTarget.EnableBillboard.ToKeywordString(1), billboardOn);

                if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kBillboardFacing) && (treeVersion == 0))
                {
                    bool billboardFacing = (material.GetInt(SpeedTreeLitOptionsUIBlock.kBillboardFacing) != 0);
                    CoreUtils.SetKeyword(material, HDSpeedTreeTarget.BillboardFaceCam.ToKeywordString(1), billboardOn && billboardFacing);
                }
            }
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
