namespace UnityEngine.Experimental.Rendering
{
    // Abstract interface for per-light game specific feature flags.
    public class HDLightCustomFlags : MonoBehaviour
    {
        [GenerateHLSL]
        public struct LightCustomData
        {
            public uint featureFlags;
        }

        public virtual LightCustomData GetLightCustomData()
        {
            return new LightCustomData { featureFlags = 0u };
        }
    }
}