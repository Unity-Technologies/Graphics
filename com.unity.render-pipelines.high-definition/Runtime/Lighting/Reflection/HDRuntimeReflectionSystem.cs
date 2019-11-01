using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDRuntimeReflectionSystem : ScriptableRuntimeReflectionSystem
    {
        static HDRuntimeReflectionSystem k_instance = new HDRuntimeReflectionSystem();

        // We must use a static constructor and only set the system in the Initialize method
        // in case this method is called multiple times.
        // This will be the case when entering play mode without performing the domain reload.
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
            => ScriptableRuntimeReflectionSystemSettings.system = k_instance;

        // Note: method bool TickRealtimeProbes(); in base will create GC.Alloc due to Unity binding code
        // (bool as return type is not handled properly)
        // Will be fixed in future release of Unity.
    }
}
