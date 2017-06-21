namespace UnityEngine.Experimental.Rendering
{
    public enum LightArchetype { Punctual, Area, Projector };

    //@TODO: We should continuously move these values
    // into the engine when we can see them being generally useful
    [RequireComponent(typeof(Light))]
    public class HDAdditionalLightData : MonoBehaviour
    {
        [Range(0.0F, 100.0F)]
        public float m_innerSpotPercent = 0.0f; // To display this field in the UI this need to be public

        public float GetInnerSpotPercent01()
        {
            return Mathf.Clamp(m_innerSpotPercent, 0.0f, 100.0f) / 100.0f;
        }

        [Range(0.0F, 1.0F)]
        public float lightDimmer = 1.0f;

        // Not used for directional lights.
        public float fadeDistance = 10000.0f;

        public bool affectDiffuse = true;
        public bool affectSpecular = true;

        public LightArchetype archetype = LightArchetype.Punctual;

        [Range(0.0f, 20.0f)]
        public float lightLength = 0.0f; // Area & projector lights

        [Range(0.0f, 20.0f)]
        public float lightWidth  = 0.0f; // Area & projector lights
    }
}
