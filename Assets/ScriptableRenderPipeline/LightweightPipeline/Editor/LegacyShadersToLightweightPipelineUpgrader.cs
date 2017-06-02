using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    public class LegacyShadersToLightweightPipelineUpgrader
    {
        [MenuItem("RenderPipeline/LightweightPipeline/Material Upgraders/Upgrade Legacy Materials to LightweightPipeline - Project", false, 3)]
        public static void UpgradeMaterialsToLDProject()
        {
            List<MaterialUpgrader> materialUpgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref materialUpgraders);

            MaterialUpgrader.UpgradeProjectFolder(materialUpgraders, "Upgrade to LD Materials");
        }

        [MenuItem("RenderPipeline/LightweightPipeline/Material Upgraders/Upgrade Legacy Materials to LightweightPipeline - Selection", false, 4)]
        public static void UpgradeMaterialsToLDSelection()
        {
            List<MaterialUpgrader> materialUpgraders = new List<MaterialUpgrader>();
            GetUpgraders(ref materialUpgraders);

            MaterialUpgrader.UpgradeSelection(materialUpgraders, "Upgrade to Lightweight Materials");
        }

        private static void GetUpgraders(ref List<MaterialUpgrader> materialUpgraders)
        {
            /////////////////////////////////////
            // Legacy Shaders upgraders         /
            /////////////////////////////////////
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Specular", SupportedUpgradeParams.specularOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Bumped Specular", SupportedUpgradeParams.specularOpaque));

            // TODO: option to use environment map as texture or use reflection probe
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Reflective/Bumped Diffuse", SupportedUpgradeParams.diffuseCubemap));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Reflective/Bumped Specular", SupportedUpgradeParams.specularOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Reflective/Diffuse", SupportedUpgradeParams.diffuseCubemap));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Reflective/Specular", SupportedUpgradeParams.specularOpaque));

            // Self-Illum upgrader
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Self-Illumin/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Self-Illumin/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Self-Illumin/Specular", SupportedUpgradeParams.specularOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Self-Illumin/Bumped Specular", SupportedUpgradeParams.specularOpaque));

            // Alpha Blended
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Diffuse", SupportedUpgradeParams.diffuseAlpha));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Specular", SupportedUpgradeParams.specularAlpha));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Bumped Diffuse", SupportedUpgradeParams.diffuseAlpha));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Bumped Specular", SupportedUpgradeParams.specularAlpha));

            // Cutout
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Cutout/Diffuse", SupportedUpgradeParams.diffuseAlphaCutout));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Cutout/Specular", SupportedUpgradeParams.specularAlphaCutout));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Cutout/Bumped Diffuse", SupportedUpgradeParams.diffuseAlphaCutout));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Legacy Shaders/Transparent/Cutout/Bumped Specular", SupportedUpgradeParams.specularAlphaCutout));

            /////////////////////////////////////
            // Reflective Shader Upgraders      /
            /////////////////////////////////////
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Reflective/Diffuse Transperant", SupportedUpgradeParams.diffuseCubemapAlpha));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Reflective/Diffuse Reflection Spec", SupportedUpgradeParams.specularCubemap));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Reflective/Diffuse Reflection Spec Transp", SupportedUpgradeParams.specularCubemapAlpha));

            /////////////////////////////////////
            // Mobile Upgraders                 /
            /////////////////////////////////////
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Diffuse", SupportedUpgradeParams.diffuseOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Bumped Specular", SupportedUpgradeParams.specularOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Bumped Specular(1 Directional Light)", SupportedUpgradeParams.specularOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/Unlit (Supports Lightmap)", SupportedUpgradeParams.diffuseOpaque));
            materialUpgraders.Add(new LegacyBlinnPhongUpgrader("Mobile/VertexLit", SupportedUpgradeParams.specularOpaque));

            /////////////////////////////////////
            // Particles                        /
            /////////////////////////////////////
            materialUpgraders.Add(new ParticlesAdditiveUpgrader("Particles/Additive"));
            materialUpgraders.Add(new ParticlesAdditiveUpgrader("Mobile/Particles/Additive"));
            materialUpgraders.Add(new ParticlesMultiplyUpgrader("Particles/Multiply"));
            materialUpgraders.Add(new ParticlesMultiplyUpgrader("Mobile/Particles/Multiply"));
        }
    }
}
