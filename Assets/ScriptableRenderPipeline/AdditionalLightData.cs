namespace UnityEngine.Experimental.Rendering
{
    public enum LightArchetype { Punctual, Area, Projector };

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    public class AdditionalLightData : MonoBehaviour
    {
        public const int DefaultShadowResolution = 512;

        public int shadowResolution = DefaultShadowResolution;

        public static int GetShadowResolution(AdditionalLightData lightData)
        {
            if (lightData != null)
                return lightData.shadowResolution;
            else
                return DefaultShadowResolution;
        }

        [Range(0.0F, 100.0F)]
        public float m_innerSpotPercent = 0.0f; // To display this field in the UI this need to be public

        public float GetInnerSpotPercent01()
        {
            return Mathf.Clamp(m_innerSpotPercent, 0.0f, 100.0f) / 100.0f;
        }

        [Range(0.0F, 1.0F)]
        public float shadowDimmer = 1.0f;
        [Range(0.0F, 1.0F)]
        public float lightDimmer = 1.0f;

        // Not used for directional lights.
        public float fadeDistance = 10000.0f;
        public float shadowFadeDistance = 10000.0f;

        public bool affectDiffuse = true;
        public bool affectSpecular = true;

        public LightArchetype archetype = LightArchetype.Punctual;
        public bool isDoubleSided = false; // Rectangular area lights only

        [Range(0.0f, 20.0f)]
        public float lightLength = 0.0f; // Area & projector lights

        [Range(0.0f, 20.0f)]
        public float lightWidth  = 0.0f; // Area & projector lights
    }
}
