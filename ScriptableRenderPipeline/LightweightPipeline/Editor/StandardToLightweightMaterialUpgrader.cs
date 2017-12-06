using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    public class StandardToLightweightMaterialUpgrader
    {
        [MenuItem("Edit/Render Pipeline/Upgrade/Lightweight/Upgrade Project Materials", priority = CoreUtils.editMenuPriority2)]
        private static void UpgradeMaterialsToLDProject()
        {
            List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref upgraders);

            MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to Lightweight Pipeline Materials");
        }

        [MenuItem("Edit/Render Pipeline/Upgrade/Lightweight/Upgrade Selected Materials", priority = CoreUtils.editMenuPriority2)]
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
        }
    }
}
