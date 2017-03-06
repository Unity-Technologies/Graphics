using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.Rendering;
using UnityEngine;

public class StandardSpecularToLDMaterialUpgrader : MaterialUpgrader
{
    [MenuItem("LDRenderPipeline/Upgrade Standard Materials to LDRenderPipeline")]
    private static void UpgradeMaterialsToLD()
    {
        List<MaterialUpgrader> upgraders = new List<MaterialUpgrader>();
        upgraders.Add(new StandardSpecularToLDMaterialUpgrader("Standard (Specular setup)"));
        upgraders.Add(new StandardSpecularToLDMaterialUpgrader("Standard"));
        upgraders.Add(new StandardSpecularToLDMaterialUpgrader("TerrainSurface"));

        MaterialUpgrader.UpgradeProjectFolder(upgraders, "Upgrade to LD Materials");
    }

    StandardSpecularToLDMaterialUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "LDRenderPipeline/Specular");
    }
}
