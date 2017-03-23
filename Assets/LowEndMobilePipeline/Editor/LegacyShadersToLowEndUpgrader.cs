using System.Collections.Generic;
using UnityEditor.Experimental.Rendering;
using UnityEditor;

public class LegacyShadersToLowEndUpgrader
{
    [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Legacy Materials to LowEndMobile - Project", false, 3)]
    public static void UpgradeMaterialsToLDProject()
    {
        List<MaterialUpgrader> materialUpgraders = new List<MaterialUpgrader>();
        GetUpgraders(ref materialUpgraders);

        MaterialUpgrader.UpgradeProjectFolder(materialUpgraders, "Upgrade to LD Materials");
    }

    [MenuItem("RenderPipeline/LowEndMobilePipeline/Material Upgraders/Upgrade Legacy Materials to LowEndMobile - Selection", false, 4)]
    public static void UpgradeMaterialsToLDSelection()
    {
        List<MaterialUpgrader> materialUpgraders = new List<MaterialUpgrader>();
        GetUpgraders(ref materialUpgraders);

        MaterialUpgrader.UpgradeSelection(materialUpgraders, "Upgrade to LD Materials");
    }

    // TODO: Replace this logic with AssignNewShaderToMaterial
    private static void GetUpgraders(ref List<MaterialUpgrader> materialUpgraders)
    {
        /////////////////////////////////////
        // Legacy Shaders upgraders         /
        /////////////////////////////////////
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Lmegacy Shaders/Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Specular"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Bumped Specular"));

        // TODO: option to use environment map as texture or use reflection probe
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Reflective/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Reflective/Bumped Specular"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Reflective/Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Reflective/Specular"));

        // Self-Illum upgrader
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Self-Illumin/Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Self-Illumin/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Self-Illumin/Specular"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Self-Illumin/Bumped Specular"));

        // Alpha Blended
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Specular"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Bumped Specular"));

        // Cutout
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Cutout/Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Cutout/Specular"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Cutout/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Cutout/Bumped Specular"));

        /////////////////////////////////////
        // Reflective Shader Upgraders      /
        /////////////////////////////////////
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Reflective/Diffuse Transperant"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Reflective/Diffuse Reflection Spec"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Reflective/Diffuse Reflection Spec Transp"));

        /////////////////////////////////////
        // Mobile Upgraders                 /
        /////////////////////////////////////
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Bumped Specular"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Bumped Specular(1 Directional Light)"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Bumped Diffuse"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Unlit (Supports Lightmap)"));
        materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/VertexLit"));

        /////////////////////////////////////
        // Particles                        /
        /////////////////////////////////////
        materialUpgraders.Add(new ParticlesAdditiveUpgrader("Particles/Additive"));
        materialUpgraders.Add(new ParticlesAdditiveUpgrader("Mobile/Particles/Additive"));
        materialUpgraders.Add(new ParticlesMultiplyUpgrader("Particles/Multiply"));
        materialUpgraders.Add(new ParticlesMultiplyUpgrader("Mobile/Particles/Multiply"));
    }
}
