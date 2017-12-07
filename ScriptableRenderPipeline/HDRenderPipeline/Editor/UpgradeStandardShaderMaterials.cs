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

        [MenuItem("Edit/Render Pipeline/Upgrade/High Definition/Upgrade Standard Materials to Lit Materials (Project)", priority = CoreUtils.editMenuPriority2)]
        static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("Edit/Render Pipeline/Upgrade/High Definition/Upgrade Standard Materials to Lit Materials (Selection)", priority = CoreUtils.editMenuPriority2)]
        static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("Edit/Render Pipeline/Upgrade/High Definition/Modify Light Intensity for Upgrade (Scene Only)", priority = CoreUtils.editMenuPriority2)]
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
