using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

public class StandardToLowEndMaterialUpgrader : MaterialUpgrader
{
    [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Standard Materials to Low End Mobile - Selection", false, 1)]
    private static void UpgradeMaterialsToLDProject()
    {
        List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
        GetUpgraders(ref upgraders);

        MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to LD Materials");
    }

    [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Standard Materials to Low End Mobile - Project Folder", false, 2)]
    private static void UpgradeMaterialsToLDSelection()
    {
        List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
        GetUpgraders(ref upgraders);

        MaterialUpgrader.UpgradeSelection(upgraders, "Upgrade to LD Materials");
    }

    private static void GetUpgraders(ref List<MaterialUpgrader> upgraders)
    {
        upgraders.Add(new StandardToLowEndMaterialUpgrader("Standard (Specular setup)"));
        upgraders.Add(new StandardToLowEndMaterialUpgrader("Standard"));
        upgraders.Add(new StandardToLowEndMaterialUpgrader("TerrainSurface"));
    }

    StandardToLowEndMaterialUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/NonPBR");
        RenameFloat("_Glossiness", "_Shininess");
    }
}
