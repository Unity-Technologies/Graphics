using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

public class StandardToLowEndMaterialUpgrader : MaterialUpgrader
{
    [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Standard Materials to Low End Mobile - Selection")]
    private static void UpgradeMaterialsToLDProject()
    {
        List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
        upgraders.Add(new StandardToLowEndMaterialUpgrader("Standard (Specular setup)"));
        upgraders.Add(new StandardToLowEndMaterialUpgrader("Standard"));
        upgraders.Add(new StandardToLowEndMaterialUpgrader("TerrainSurface"));

        MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to LD Materials");
    }

    [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Standard Materials to Low End Mobile - Project Folder")]
    private static void UpgradeMaterialsToLDSelection()
    {
        List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
        upgraders.Add(new StandardToLowEndMaterialUpgrader("Standard (Specular setup)"));
        upgraders.Add(new StandardToLowEndMaterialUpgrader("Standard"));
        upgraders.Add(new StandardToLowEndMaterialUpgrader("TerrainSurface"));

        MaterialUpgrader.UpgradeSelection(upgraders, "Upgrade to LD Materials");
    }

    StandardToLowEndMaterialUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile");
    }
}
