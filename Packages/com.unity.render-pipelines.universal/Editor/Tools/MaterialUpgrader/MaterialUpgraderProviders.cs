using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class StandardMaterialUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new StandardUpgrader("Standard");
            yield return new StandardUpgrader("Standard (Specular setup)");
        }
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class StandardSimpleLightingUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Diffuse Detail", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Diffuse Fast", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Specular", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Bumped Specular", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Parallax Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Parallax Specular", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/VertexLit", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/VertexLit", SupportedUpgradeParams.specularAlphaCutout);

            // Reflective
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped Diffuse", SupportedUpgradeParams.diffuseCubemap);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped Specular", SupportedUpgradeParams.specularCubemap);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped Unlit", SupportedUpgradeParams.diffuseCubemap);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Bumped VertexLit", SupportedUpgradeParams.diffuseCubemap);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Diffuse", SupportedUpgradeParams.diffuseCubemap);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Specular", SupportedUpgradeParams.specularCubemap);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/VertexLit", SupportedUpgradeParams.diffuseCubemap);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Parallax Diffuse", SupportedUpgradeParams.diffuseCubemap);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Reflective/Parallax Specular", SupportedUpgradeParams.specularCubemap);

            // Self-Illum
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Parallax Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Specular", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Bumped Specular", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/Parallax Specular", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Self-Illumin/VertexLit", SupportedUpgradeParams.specularOpaque);

            // Alpha Blended
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Diffuse", SupportedUpgradeParams.diffuseAlpha);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Specular", SupportedUpgradeParams.specularAlpha);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Bumped Diffuse", SupportedUpgradeParams.diffuseAlpha);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Bumped Specular", SupportedUpgradeParams.specularAlpha);

            // Cutout
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/Diffuse", SupportedUpgradeParams.diffuseAlphaCutout);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/Specular", SupportedUpgradeParams.specularAlphaCutout);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/Bumped Diffuse", SupportedUpgradeParams.diffuseAlphaCutout);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Transparent/Cutout/Bumped Specular", SupportedUpgradeParams.specularAlphaCutout);

            // Lightmapped
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/Specular", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/VertexLit", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Legacy Shaders/Lightmapped/Bumped Specular", SupportedUpgradeParams.specularOpaque);
        }
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class SpriteMaterialUpgraderProviders : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new StandardSimpleLightingUpgrader("Sprites/Diffuse", SupportedUpgradeParams.diffuseAlpha);
        }
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class UIMaterialUpgraderProviders : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new StandardSimpleLightingUpgrader("UI/Lit/Bumped", SupportedUpgradeParams.diffuseAlphaCutout);
            yield return new StandardSimpleLightingUpgrader("UI/Lit/Detail", SupportedUpgradeParams.diffuseAlphaCutout);
            yield return new StandardSimpleLightingUpgrader("UI/Lit/Refraction", SupportedUpgradeParams.diffuseAlphaCutout);
            yield return new StandardSimpleLightingUpgrader("UI/Lit/Refraction Detail", SupportedUpgradeParams.diffuseAlphaCutout);
            yield return new StandardSimpleLightingUpgrader("UI/Lit/Transparent", SupportedUpgradeParams.diffuseAlpha);
        }
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class MobileMaterialUpgraderProviders : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new StandardSimpleLightingUpgrader("Mobile/Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Mobile/Bumped Specular", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Mobile/Bumped Specular (1 Directional Light)", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Mobile/Bumped Diffuse", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Mobile/Unlit (Supports Lightmap)", SupportedUpgradeParams.diffuseOpaque);
            yield return new StandardSimpleLightingUpgrader("Mobile/VertexLit", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Mobile/VertexLit (Only Directional Lights)", SupportedUpgradeParams.specularOpaque);
            yield return new StandardSimpleLightingUpgrader("Mobile/Particles/VertexLit Blended", SupportedUpgradeParams.specularOpaque);

        }
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class TerrainMaterialUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new TerrainUpgrader("Nature/Terrain/Standard");
            yield return new SpeedTreeUpgrader("Nature/SpeedTree");
            yield return new SpeedTreeBillboardUpgrader("Nature/SpeedTree Billboard");
            yield return new UniversalSpeedTree8Upgrader("Nature/SpeedTree8");
        }
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class ParticleMaterialUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            // Standard particle shaders
            yield return new ParticleUpgrader("Particles/Standard Surface");
            yield return new ParticleUpgrader("Particles/Standard Unlit");
            yield return new ParticleUpgrader("Particles/VertexLit Blended");
        }
    }

    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    class AutodeskMaterialUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new AutodeskInteractiveUpgrader("Autodesk Interactive");
        }
    }
}
