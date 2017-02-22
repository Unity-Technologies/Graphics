using System.Collections.Generic;
using UnityEditor.Experimental.Rendering;
using UnityEditor;

public class LegacyBumpSpecularToLDMaterialUpgrader : MaterialUpgrader
{
    [MenuItem("LDRenderPipeline/Upgrade Legacy Shaders to LDRenderPipeline Materials")]
    public static void UpgradeMaterialsToLD()
    {
        List<MaterialUpgrader> materialUpgraders = new List<MaterialUpgrader>();
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Mobile/Bumped Specular"));
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Mobile/Bumped Specular(1 Directional Light)"));
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Legacy Shaders/Lightmapped/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Legacy Shaders/Lightmapped/Bumped Specular"));
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Legacy Shaders/Lightmapped/Diffuse"));
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Legacy Shaders/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Legacy Shaders/Bumped Specular"));
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Legacy Shaders/Diffuse"));
        materialUpgraders.Add(new LegacyBumpSpecularToLDMaterialUpgrader("Legacy Shaders/Lightmapped/Bumped Specular"));

        MaterialUpgrader.UpgradeProjectFolder(materialUpgraders, "Upgrade to LD Materials");
    }

    LegacyBumpSpecularToLDMaterialUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "LDRenderPipeline/Specular");
        RenameFloat("_Shininess", "_Glossiness");
    }
}
