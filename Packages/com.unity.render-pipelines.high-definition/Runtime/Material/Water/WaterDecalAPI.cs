using static UnityEngine.Rendering.HighDefinition.HDShaderIDs;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class WaterDecalAPI
    {
        internal static bool AffectsOutput(Material material, int property) => material.HasProperty(property) && material.GetFloat(property) == 1.0f;

        internal static void SetupWaterDecalKeywordsAndProperties(Material material)
        {
            CoreUtils.SetKeyword(material, "_AFFECTS_DEFORMATION", AffectsOutput(material, _AffectDeformation));
            CoreUtils.SetKeyword(material, "_AFFECTS_FOAM", AffectsOutput(material, _AffectsFoam));
            CoreUtils.SetKeyword(material, "_AFFECTS_MASK", AffectsOutput(material, _AffectsSimulationMask));
            CoreUtils.SetKeyword(material, "_AFFECTS_LARGE_CURRENT", AffectsOutput(material, _AffectsLargeCurrent));
            CoreUtils.SetKeyword(material, "_AFFECTS_RIPPLES_CURRENT", AffectsOutput(material, _AffectsRipplesCurrent));
        }
    }
}
