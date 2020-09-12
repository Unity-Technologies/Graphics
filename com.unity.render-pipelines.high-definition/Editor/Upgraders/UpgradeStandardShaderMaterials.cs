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
            return upgraders;
        }

        [MenuItem("Edit/Render Pipeline/Upgrade Project Materials to High Definition Materials", priority = CoreUtils.editMenuPriority2)]
        internal static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("Edit/Render Pipeline/Upgrade Selected Materials to High Definition Materials", priority = CoreUtils.editMenuPriority2)]
        internal static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("Edit/Render Pipeline/Multiply Unity Builtin Directional Light Intensity to match High Definition", priority = CoreUtils.editMenuPriority2)]
        internal static void UpgradeLights()
        {
            Light[] lights = Light.GetLights(LightType.Directional, 0);
            foreach (var l in lights)
            {
                Undo.RecordObject(l, "Light intensity x PI");
                l.intensity *= Mathf.PI;
            }
        }

        [MenuItem("Edit/Render Pipeline/Upgrade HDRP Materials to Latest Version", priority = CoreUtils.editMenuPriority2)]
        internal static void UpgradeMaterials()
        {
            // Force reimport of all materials, this will upgrade the needed one and save the assets if needed
            MaterialReimporter.ReimportAllMaterials();
        }
    }
}
