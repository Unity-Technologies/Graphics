namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDRuntimeReflectionSystem : ScriptableRuntimeReflectionSystem
    {
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            ScriptableRuntimeReflectionSystemSettings.system = new HDRuntimeReflectionSystem();
        }

        // Note: method bool TickRealtimeProbes(); in base will create GC.Alloc due to Unity binding code
        // (bool as return type is not handled properly)
        // Will be fixed in future release of Unity.
    }
}
