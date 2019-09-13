using UnityEngine;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

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
            new SpeedTreeLitOptionsUIBlock(MaterialUIBlock.Expandable.Base),
            new ShaderGraphUIBlock(MaterialUIBlock.Expandable.ShaderGraph),
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

        public static void SetupMaterialKeywordsAndPass(Material material)
        {
            SynchronizeShaderGraphProperties(material);
            BaseLitGUI.SetupBaseLitKeywords(material);
            bool receiveSSR = material.HasProperty(kReceivesSSR) ? material.GetInt(kReceivesSSR) != 0 : false;
            bool useSplitLighting = material.HasProperty(kUseSplitLighting) ? material.GetInt(kUseSplitLighting) != 0: false;
            BaseLitGUI.SetupStencil(material, receiveSSR, useSplitLighting);
            if (material.HasProperty(kAdditionalVelocityChange))
            {
                CoreUtils.SetKeyword(material, "_ADDITIONAL_VELOCITY_CHANGE", material.GetInt(kAdditionalVelocityChange) != 0);
            }

            if (material.HasProperty("_SpeedTreeGeom"))
            {
                SpeedTreeLitMasterNode.TreeGeomType v = (SpeedTreeLitMasterNode.TreeGeomType)material.GetInt("_SpeedTreeGeom");
                // Initially assume that we're not the default type.
                material.DisableKeyword("GEOM_TYPE_BRANCH");

                switch (v)
                {
                    case SpeedTreeLitMasterNode.TreeGeomType.BranchDetail:
                        material.EnableKeyword("GEOM_TYPE_BRANCH_DETAIL");
                        //material.EnableKeyword("GEOM_TYPE_BRANCH");
                        break;
                    case SpeedTreeLitMasterNode.TreeGeomType.Branch:
                        material.EnableKeyword("GEOM_TYPE_BRANCH");
                        break;
                    case SpeedTreeLitMasterNode.TreeGeomType.Frond:
                        material.EnableKeyword("GEOM_TYPE_FROND");
                        break;
                    case SpeedTreeLitMasterNode.TreeGeomType.Leaf:
                        material.EnableKeyword("GEOM_TYPE_LEAF");
                        break;
                    case SpeedTreeLitMasterNode.TreeGeomType.Mesh:
                        material.EnableKeyword("GEOM_TYPE_MESH");
                        break;
                }
            }

            if (material.HasProperty("_WindEnabled"))
            {
                int windOn = material.GetInt("_WindEnabled");
                if (windOn == 0)
                    material.DisableKeyword("ENABLE_WIND");
            }

            if (material.HasProperty("_WindQuality") && material.IsKeywordEnabled("SPEEDTREE_V8"))
            {
                SpeedTreeLitMasterNode.WindQuality q = (SpeedTreeLitMasterNode.WindQuality)material.GetInt("_WindQuality");

                switch (q)
                {
                    case SpeedTreeLitMasterNode.WindQuality.None:
                        material.EnableKeyword("_WINDQUALITY_NONE");
                        break;
                    case SpeedTreeLitMasterNode.WindQuality.Fastest:
                        material.EnableKeyword("_WINDQUALITY_FASTEST");
                        break;
                    case SpeedTreeLitMasterNode.WindQuality.Fast:
                        material.EnableKeyword("_WINDQUALITY_FAST");
                        break;
                    case SpeedTreeLitMasterNode.WindQuality.Better:
                        material.EnableKeyword("_WINDQUALITY_BETTER");
                        break;
                    case SpeedTreeLitMasterNode.WindQuality.Best:
                        material.EnableKeyword("_WINDQUALITY_BEST");
                        break;
                    case SpeedTreeLitMasterNode.WindQuality.Palm:
                        material.EnableKeyword("_WINDQUALITY_PALM");
                        break;
                }
            }

            if (material.HasProperty("_Billboard"))
            {
                int b = material.GetInt("_Billboard");
                if (b != 0)
                    material.EnableKeyword("EFFECT_BILLBOARD");
            }
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
