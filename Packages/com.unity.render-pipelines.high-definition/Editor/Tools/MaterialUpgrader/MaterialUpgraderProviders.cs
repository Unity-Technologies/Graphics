using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class StandardsToHDLitMaterialUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new StandardsToHDLitMaterialUpgrader("Standard", "HDRP/Lit");
            yield return new StandardsToHDLitMaterialUpgrader("Standard (Specular setup)", "HDRP/Lit");
            yield return new StandardsToHDLitMaterialUpgrader("Autodesk Interactive", "HDRP/Lit");
        }
    }

    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class UnlitsToHDUnlitUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new UnlitsToHDUnlitUpgrader("Unlit/Color", "HDRP/Unlit");
            yield return new UnlitsToHDUnlitUpgrader("Unlit/Texture", "HDRP/Unlit");
            yield return new UnlitsToHDUnlitUpgrader("Unlit/Transparent", "HDRP/Unlit");
            yield return new UnlitsToHDUnlitUpgrader("Unlit/Transparent Cutout", "HDRP/Unlit");
        }
    }

    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class TerrianUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new StandardsTerrainToHDTerrainLitUpgrader("Nature/Terrain/Standard", "HDRP/TerrainLit");
        }
    }

    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class SpeedTreeUpgraderProvider : IMaterialUpgradersProvider
    {
        public IEnumerable<MaterialUpgrader> GetUpgraders()
        {
            yield return new HDSpeedTree8MaterialUpgrader("Nature/SpeedTree8", "HDRP/Nature/SpeedTree8");
        }
    }
}
