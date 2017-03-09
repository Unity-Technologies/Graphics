using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.Rendering;

public class LegacyMobileToLowendMobileUpgrader : MaterialUpgrader
{
    [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Legacy Mobile Materials to LowEndMobile")]
    public static void UpgradeMaterialsToLD()
    {
        List<MaterialUpgrader> materialUpgraders = new List<MaterialUpgrader>();
        materialUpgraders.Add(new LegacyMobileToLowendMobileUpgrader("Mobile/Diffuse"));
        materialUpgraders.Add(new LegacyMobileToLowendMobileUpgrader("Mobile/Bumped Specular"));
        materialUpgraders.Add(new LegacyMobileToLowendMobileUpgrader("Mobile/Bumped Specular(1 Directional Light)"));
        materialUpgraders.Add(new LegacyMobileToLowendMobileUpgrader("Mobile/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyMobileToLowendMobileUpgrader("Mobile/Unlit (Supports Lightmap)"));
        materialUpgraders.Add(new LegacyMobileToLowendMobileUpgrader("Mobile/VertexLit"));

        MaterialUpgrader.UpgradeProjectFolder(materialUpgraders, "Upgrade to LD Materials");
    }

    LegacyMobileToLowendMobileUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile");
        RenameFloat("_Shininess", "_Glossiness");
    }
}
