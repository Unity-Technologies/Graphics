namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDRuntimeReflectionSystem : ScriptableRuntimeReflectionSystem
    {
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            ScriptableRuntimeReflectionSystemSettings.system = new HDRuntimeReflectionSystem();
        }
    }
}
