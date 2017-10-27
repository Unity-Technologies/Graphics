using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    public class StandardToLightweightMaterialUpgrader
    {
        [MenuItem("RenderPipeline/Lightweight Pipeline/Material Upgraders/Upgrade Project Materials", false, 1)]
        private static void UpgradeMaterialsToLDProject()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to Lightweight Pipeline Materials");
        }

        [MenuItem("RenderPipeline/Lightweight Pipeline/Material Upgraders/Upgrade Selected Materials", false, 2)]
        private static void UpgradeMaterialsToLDSelection()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeSelection(upgraders, "Upgrade to Lightweight Pipeline Materials");
        }

        private static void GetUpgraders(ref List<MaterialUpgrader> upgraders)
        {
            upgraders.Add(new StandardUpgrader("Standard (Specular setup)"));
            upgraders.Add(new StandardUpgrader("Standard"));
            upgraders.Add(new TerrainUpgrader("TerrainSurface"));
        }
    }
}
