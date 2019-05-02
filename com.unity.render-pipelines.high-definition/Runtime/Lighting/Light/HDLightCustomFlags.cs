namespace UnityEngine.Experimental.Rendering
{
    // Abstract interface for per-light game specific feature flags.
    public class HDLightCustomFlags : MonoBehaviour
    {
        public struct LightCustomData
        {
            public uint customFeatureFlags;
            public float customRadiusScale;
            public float customRadiusBias;
            public float customPadding;
        }

        public virtual LightCustomData GetLightCustomData()
        {
            return new LightCustomData
            {
                customFeatureFlags = 0u,
                customRadiusScale = 1.0f,
                customRadiusBias = 0.0f,
                customPadding = 0.0f
            };
        }
    }
}
