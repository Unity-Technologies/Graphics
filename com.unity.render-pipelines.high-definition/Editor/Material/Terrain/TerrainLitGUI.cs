using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Terrain Lit materials (does not include ShaderGraphs)
    /// </summary>
    class TerrainLitGUI : TerrainGUI, ITerrainLayerCustomUI
    {
        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupTerrainLitKeywordsAndPass(Material material)
        {
            BaseLitGUI.SetupBaseLitKeywords(material);
            BaseLitGUI.SetupBaseLitMaterialPass(material);
            bool receiveSSR = material.GetSurfaceType() == SurfaceType.Opaque ? (material.HasProperty(kReceivesSSR) ? material.GetInt(kReceivesSSR) != 0 : false)
                : (material.HasProperty(kReceivesSSRTransparent) ? material.GetInt(kReceivesSSRTransparent) != 0 : false);
            BaseLitGUI.SetupStencil(material, receiveSSR, material.GetMaterialId() == MaterialId.LitSSS);

            SetupTerrainKeywordsAndPass(material);
        }

        bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
        {
            return TerrainLayerGUI(terrainLayer, terrain);
        }

        public override void ValidateMaterial(Material material) => SetupTerrainLitKeywordsAndPass(material);
    }
} // namespace UnityEditor
