using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    class UpgradeStandardShaderMaterials
    {
        static List<MaterialUpgrader> GetHDUpgraders()
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(new StandardsToHDLitMaterialUpgrader("Standard", "HDRP/Lit"));
            upgraders.Add(new StandardsToHDLitMaterialUpgrader("Standard (Specular setup)", "HDRP/Lit"));
            upgraders.Add(new StandardsToHDLitMaterialUpgrader("Standard (Roughness setup)", "HDRP/Lit"));

            upgraders.Add(new UnlitsToHDUnlitUpgrader("Unlit/Color", "HDRP/Unlit"));
            upgraders.Add(new UnlitsToHDUnlitUpgrader("Unlit/Texture", "HDRP/Unlit"));
            upgraders.Add(new UnlitsToHDUnlitUpgrader("Unlit/Transparent", "HDRP/Unlit"));
            upgraders.Add(new UnlitsToHDUnlitUpgrader("Unlit/Transparent Cutout", "HDRP/Unlit"));

            upgraders.Add(new StandardsTerrainToHDTerrainLitUpgrader("Nature/Terrain/Standard", "HDRP/TerrainLit"));

            return upgraders;
        }

        [MenuItem("Edit/Render Pipeline/HD Render Pipeline/Upgrade from Builtin pipeline/Upgrade Project Materials to High Definition Materials")]
        internal static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("Edit/Render Pipeline/HD Render Pipeline/Upgrade from Builtin pipeline/Upgrade Selected Materials to High Definition Materials")]
        internal static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("Edit/Render Pipeline/HD Render Pipeline/Upgrade from Builtin pipeline/Upgrade Scene Terrains to High Definition Terrains")]
        static void UpgradeSceneTerrainsToHighDefinitionTerrains(MenuCommand menuCommand)
        {
            var LegacyDefaultTerrainMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Terrain-Standard.mat");
            var HDRPTerrainMat =  AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat");
            var terrainArray = UnityEngine.GameObject.FindObjectsOfType<Terrain>();

            if (terrainArray.Length == 0)
            {
                Debug.LogWarning("No terrains were found in the scene.");
                return;
            }

            foreach (Terrain currentTerrain in terrainArray)
            {
                if (currentTerrain.materialTemplate == LegacyDefaultTerrainMat)
                {
                    currentTerrain.materialTemplate = HDRPTerrainMat;
                }
            }
        }
    }
}
