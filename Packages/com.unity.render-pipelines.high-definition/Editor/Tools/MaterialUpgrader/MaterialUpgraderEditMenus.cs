using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class MaterialUpgraderEditMenus
    {
        public static List<MaterialUpgrader> GetHDUpgraders()
        {
            return MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(HDRenderPipelineAsset));
        }

        [MenuItem("Edit/Rendering/Materials/Convert All Materials using HDRP upgraders", priority = CoreUtils.Priorities.editMenuPriority + 1)]
        internal static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetHDUpgraders(), "Upgrade to HDRP Material");
        }

        [MenuItem("Edit/Rendering/Materials/Convert Selected Materials using HDRP upgraders", true)]
        static bool MaterialValidate(MenuCommand command)
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is not Material) return false;
            }

            return true;
        }

        [MenuItem("Edit/Rendering/Materials/Convert Selected Materials using HDRP upgraders", priority = CoreUtils.Priorities.editMenuPriority + 2)]
        internal static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetHDUpgraders(), "Upgrade to HDRP Material");
        }
    }
}
