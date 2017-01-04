using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using NUnit.Framework;

namespace UnityEditor.Experimental.ScriptableRenderLoop
{
    public class UpgradeStandardShaderMaterials
    {
        static List<MaterialUpgrader> GetHDUpgraders()
        {
            var upgraders = new List<MaterialUpgrader>();
            upgraders.Add(new StandardToHDLitMaterialUpgrader());
            upgraders.Add(new StandardSpecularToHDLitMaterialUpgrader());
            return upgraders;
        }

        [MenuItem("HDRenderPipeline/Upgrade Materials - Project")]
        static void UpgradeMaterialsProject()
        {
            MaterialUpgrader.UpgradeProjectFolder(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("HDRenderPipeline/Upgrade Materials - Selection")]
        static void UpgradeMaterialsSelection()
        {
            MaterialUpgrader.UpgradeSelection(GetHDUpgraders(), "Upgrade to HD Material");
        }

        [MenuItem("HDRenderPipeline/Modify Light Intensity for Upgrade - Scene Only")]
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
