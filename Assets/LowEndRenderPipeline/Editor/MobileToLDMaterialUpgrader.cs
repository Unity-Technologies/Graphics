using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

public class MobileToLDMaterialUpgrader : MaterialUpgrader
{
    [MenuItem("LDRenderPipeline/Upgrade Mobile Materials to LDRenderPipeline")]
    public static void UpgradeMaterialsToLD()
    {
        List<MaterialUpgrader> materialUpgraders = new List<MaterialUpgrader>();
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Diffuse")); 
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Bumped Specular")); 
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Bumped Specular(1 Directional Light)")); 
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Bumped Diffuse")); 
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Unlit (Supports Lightmap)")); 
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/VertexLit"));

        MaterialUpgrader.UpgradeProjectFolder(materialUpgraders, "Upgrade to LD Materials");
    }

    MobileToLDMaterialUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "LDRenderPipeline/Specular");
        RenameFloat("_Shininess", "_Glossiness");
    }
}
