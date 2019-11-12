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
            new SpeedTreeLitOptionsUIBlock(MaterialUIBlock.Expandable.Other),
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
            BaseLitGUI.SetupBaseLitMaterialPass(material);
            bool receiveSSR = material.HasProperty(kReceivesSSR) ? material.GetInt(kReceivesSSR) != 0 : false;
            bool useSplitLighting = material.HasProperty(kUseSplitLighting) ? material.GetInt(kUseSplitLighting) != 0 : false;
            BaseLitGUI.SetupStencil(material, receiveSSR, useSplitLighting);
            if (material.HasProperty(kAddPrecomputedVelocity))
            {
                CoreUtils.SetKeyword(material, "_ADD_PRECOMPUTED_VELOCITY", material.GetInt(kAddPrecomputedVelocity) != 0);
            }

            // Because of the fact that the Master Node does not setup GeomType as a property
            // when not using the appropriate SpeedTree Asset version, we can assume
            // that detection of the property itself is sufficient to establish that we're in
            // the right mode.
            if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kGeomType))
            {
                SpeedTreeLitMasterNode.SpeedTreeGeomType v = (SpeedTreeLitMasterNode.SpeedTreeGeomType)material.GetInt(SpeedTreeLitOptionsUIBlock.kGeomType);
                // Make sure to clear out all the potential options before setting the correct one.
                // Avoids duplication and/or overlap -- this is especially important for wind.
                material.DisableKeyword("GEOM_TYPE_BRANCH");
                material.DisableKeyword("GEOM_TYPE_BRANCH_DETAIL");
                material.DisableKeyword("GEOM_TYPE_FROND");
                material.DisableKeyword("GEOM_TYPE_LEAF");
                material.DisableKeyword("GEOM_TYPE_MESH");

                switch (v)
                {
                    case SpeedTreeLitMasterNode.SpeedTreeGeomType.BranchDetail:
                        material.EnableKeyword("GEOM_TYPE_BRANCH_DETAIL");
                        break;
                    case SpeedTreeLitMasterNode.SpeedTreeGeomType.Branch:
                        material.EnableKeyword("GEOM_TYPE_BRANCH");
                        break;
                    case SpeedTreeLitMasterNode.SpeedTreeGeomType.Frond:
                        material.EnableKeyword("GEOM_TYPE_FROND");
                        break;
                    case SpeedTreeLitMasterNode.SpeedTreeGeomType.Leaf:
                        material.EnableKeyword("GEOM_TYPE_LEAF");
                        break;
                    case SpeedTreeLitMasterNode.SpeedTreeGeomType.Mesh:
                        material.EnableKeyword("GEOM_TYPE_MESH");
                        break;
                }
            }

            
            if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kWindEnable))
            {
                bool windOn = material.GetInt(SpeedTreeLitOptionsUIBlock.kWindEnable) != 0;
                CoreUtils.SetKeyword(material, "ENABLE_WIND", windOn);

                // This is something we only have to do for Speedtree version 8, whereas 7 uses the _WindQuality value directly.
                // and it so happens that only Speedtree 7 has a _SpeedTreeGeom property, whereas 8 embeds it inside UV...
                // So we know that if we don't have a _SpeedTreeGeom property, this must be SpeedTree 8.

                if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kWindEnable) && windOn && !material.HasProperty(SpeedTreeLitOptionsUIBlock.kGeomType))
                {
                    SpeedTreeLitMasterNode.SpeedTreeWindQuality q = (SpeedTreeLitMasterNode.SpeedTreeWindQuality)material.GetInt(SpeedTreeLitOptionsUIBlock.kWindQuality);

                    material.DisableKeyword("_WINDQUALITY_NONE");
                    material.DisableKeyword("_WINDQUALITY_FASTEST");
                    material.DisableKeyword("_WINDQUALITY_FAST");
                    material.DisableKeyword("_WINDQUALITY_BETTER");
                    material.DisableKeyword("_WINDQUALITY_BEST");
                    material.DisableKeyword("_WINDQUALITY_PALM");

                    switch (q)
                    {
                        case SpeedTreeLitMasterNode.SpeedTreeWindQuality.None:
                            material.EnableKeyword("_WINDQUALITY_NONE");
                            break;
                        case SpeedTreeLitMasterNode.SpeedTreeWindQuality.Fastest:
                            material.EnableKeyword("_WINDQUALITY_FASTEST");
                            break;
                        case SpeedTreeLitMasterNode.SpeedTreeWindQuality.Fast:
                            material.EnableKeyword("_WINDQUALITY_FAST");
                            break;
                        case SpeedTreeLitMasterNode.SpeedTreeWindQuality.Better:
                            material.EnableKeyword("_WINDQUALITY_BETTER");
                            break;
                        case SpeedTreeLitMasterNode.SpeedTreeWindQuality.Best:
                            material.EnableKeyword("_WINDQUALITY_BEST");
                            break;
                        case SpeedTreeLitMasterNode.SpeedTreeWindQuality.Palm:
                            material.EnableKeyword("_WINDQUALITY_PALM");
                            break;
                    }
                }

                if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kIsBillboard))
                {
                    bool billboardOn = material.GetInt(SpeedTreeLitOptionsUIBlock.kIsBillboard) != 0;
                    CoreUtils.SetKeyword(material, "EFFECT_BILLBOARD", billboardOn);

                    if (material.HasProperty(SpeedTreeLitOptionsUIBlock.kBillboardFacing))
                    {
                        CoreUtils.SetKeyword(material, "BILLBOARD_FACE_CAMERA_POS", billboardOn && (material.GetInt(SpeedTreeLitOptionsUIBlock.kBillboardFacing) != 0));
                    }
                }
            }
        }

        protected override void SetupMaterialKeywordsAndPassInternal(Material material) => SetupMaterialKeywordsAndPass(material);
    }
}
