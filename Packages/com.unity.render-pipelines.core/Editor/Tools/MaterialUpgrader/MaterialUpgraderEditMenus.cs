using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class MaterialUpgraderEditMenus
    {
        private static readonly List<MaterialUpgrader> s_Empty = new List<MaterialUpgrader>();

        public static List<MaterialUpgrader> GetCurrentSRPUpgraders()
        {
            if (!GraphicsSettings.isScriptableRenderPipelineEnabled)
                return s_Empty;

            return MaterialUpgrader.FetchAllUpgradersForPipeline(GraphicsSettings.currentRenderPipelineAssetType);
        }

        [MenuItem("Edit/Rendering/Materials/Convert All Built-In Materials to Current SRP", true)]
        internal static bool UpgradeMaterialsProjectValidate()
        {
            return GraphicsSettings.isScriptableRenderPipelineEnabled;
        }

        [MenuItem("Edit/Rendering/Materials/Convert All Built-In Materials to Current SRP", priority = CoreUtils.Priorities.editMenuPriority + 1)]
        internal static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetCurrentSRPUpgraders(), "Upgrade to SRP Material");
        }

        [MenuItem("Edit/Rendering/Materials/Convert Selected Built-In Materials to Current SRP", true)]
        internal static bool UpgradeMaterialsSelectionValidate()
        {
            if (Selection.objects.Length == 0 || !GraphicsSettings.isScriptableRenderPipelineEnabled)
                return false;

            foreach (var obj in Selection.objects)
            {
                if (obj is not Material)
                    return false;
            }

            return true;
        }

        [MenuItem("Edit/Rendering/Materials/Convert Selected Built-In Materials to Current SRP", priority = CoreUtils.Priorities.editMenuPriority + 2)]
        internal static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetCurrentSRPUpgraders(), "Upgrade to SRP Material");
        }
    }
}
