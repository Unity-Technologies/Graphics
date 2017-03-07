using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

public class StandardToLowEndMaterialUpgrader : MaterialUpgrader
{
    [MenuItem("LowEndMobilePipeline/Material Upgraders/Upgrade Standard Materials to Low End Mobile")]
    private static void UpgradeMaterialsToLD()
    {
        List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
        upgraders.Add(new StandardToLowEndMaterialUpgrader("Standard (Specular setup)"));
        upgraders.Add(new StandardToLowEndMaterialUpgrader("Standard"));
        upgraders.Add(new StandardToLowEndMaterialUpgrader("TerrainSurface"));

        MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to LD Materials");
    }

    StandardToLowEndMaterialUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile");
    }
}
