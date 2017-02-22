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
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Diffuse")); // _MainTex
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Bumped Specular")); // _Shininess, _MainTex, _BumpMap
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Bumped Specular(1 Directional Light)")); // ""
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Bumped Diffuse")); // _MainTex, _BumpMap
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/Unlit (Supports Lightmap)")); // _MainTex
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("Mobile/VertexLit")); // MainTex
        materialUpgraders.Add(new MobileToLDMaterialUpgrader("TerrainSurface"));

        MaterialUpgrader.UpgradeProjectFolder(materialUpgraders, "Upgrade to LD Materials");
    }

    MobileToLDMaterialUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "LDRenderPipeline/Specular");
        RenameFloat("_Shininess", "_Glossiness");
    }
}
