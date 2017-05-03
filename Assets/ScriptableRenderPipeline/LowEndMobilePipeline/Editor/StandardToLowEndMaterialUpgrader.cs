using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.LowendMobile
{
    public class StandardToLowEndMaterialUpgrader
    {
        [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Standard Materials to Low End Mobile - Project Folder", false, 1)]
        private static void UpgradeMaterialsToLDProject()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to LD Materials");
        }

        [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Standard Materials to Low End Mobile - Selection", false, 2)]
        private static void UpgradeMaterialsToLDSelection()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeSelection(upgraders, "Upgrade to LD Materials");
        }

        private static void GetUpgraders(ref List<MaterialUpgrader> upgraders)
        {
            upgraders.Add(new StandardUpgrader("Standard (Specular setup)"));
            upgraders.Add(new StandardUpgrader("Standard"));
            upgraders.Add(new TerrainUpgrader("TerrainSurface"));
        }
    }
}
