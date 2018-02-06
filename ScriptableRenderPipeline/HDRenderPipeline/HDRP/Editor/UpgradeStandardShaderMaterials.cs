using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class UpgradeStandardShaderMaterials
    {
        static List<MaterialUpgrader> GetHDUpgraders()
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(new StandardToHDLitMaterialUpgrader("Standard", "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass));
            upgraders.Add(new StandardSpecularToHDLitMaterialUpgrader("Standard (Specular setup)", "HDRenderPipeline/Lit", LitGUI.SetupMaterialKeywordsAndPass));
            return upgraders;
        }

        [MenuItem("Edit/Render Pipeline/Upgrade Project Materials to High Definition Materials", priority = CoreUtils.editMenuPriority2)]
        static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("Edit/Render Pipeline/Upgrade Selected Materials to High Definition Materials", priority = CoreUtils.editMenuPriority2)]
        static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("Edit/Render Pipeline/Upgrade Scene Light Intensity for High Definition", priority = CoreUtils.editMenuPriority2)]
        static void UpgradeLights()
        {
            Light[] lights = Light.GetLights(LightType.Directional, 0);
            foreach (var l in lights)
            {
                l.intensity *= Mathf.PI;
            }
        }
    }
}
