// We need to update the culling state of all active probe once per frame
// To do so, we use a private API of the BuiltinRuntimeReflectionSystem as a workaround
// However, a clean API is coming and we will be able to replace the BuiltinUpdate call.
//#define REFLECTION_PROBE_UPDATE_CACHED_DATA_AVAILABLE
#if !REFLECTION_PROBE_UPDATE_CACHED_DATA_AVAILABLE
using System;
using System.Reflection;
#endif

using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDRuntimeReflectionSystem : ScriptableRuntimeReflectionSystem
    {
        #if !REFLECTION_PROBE_UPDATE_CACHED_DATA_AVAILABLE
        static MethodInfo BuiltinUpdate;

        static HDRuntimeReflectionSystem()
        {
            var type =
                Type.GetType("UnityEngine.Experimental.Rendering.BuiltinRuntimeReflectionSystem,UnityEngine");
            var method = type.GetMethod("BuiltinUpdate", BindingFlags.Static | BindingFlags.NonPublic);
            BuiltinUpdate = method;
        }
        #endif

        static HDRuntimeReflectionSystem k_instance = new HDRuntimeReflectionSystem();

        // We must use a static constructor and only set the system in the Initialize method
        // in case this method is called multiple times.
        // This will be the case when entering play mode without performing the domain reload.
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)
                ScriptableRuntimeReflectionSystemSettings.system = k_instance;
        }

        // Note: method bool TickRealtimeProbes() will create GC.Alloc due to Unity binding code
        // (bool as return type is not handled properly)
        // Will be fixed in future release of Unity.

        public override bool TickRealtimeProbes()
        {
            #if REFLECTION_PROBE_UPDATE_CACHED_DATA_AVAILABLE
            ReflectionProbe.UpdateCachedState();
            #else
            BuiltinUpdate.Invoke(null, new object[0]);
            #endif
            return base.TickRealtimeProbes();
        }
    }
}
