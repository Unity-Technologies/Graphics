using UnityEngine.Experimental.Rendering.HDPipeline.Internal;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static class ReflectionSystem
    {
        static ReflectionSystemInternal s_Instance = new ReflectionSystemInternal(ReflectionSystemParameters.Default, null);

        public static void SetParameters(ReflectionSystemParameters parameters)
        {
            s_Instance = new ReflectionSystemInternal(parameters, s_Instance);
        }

        public static void RegisterProbe(PlanarReflectionProbe planarProbe)
        {
            s_Instance.RegisterProbe(planarProbe);
        }

        public static void UnregisterProbe(PlanarReflectionProbe planarProbe)
        {
            s_Instance.UnregisterProbe(planarProbe);
        }

        public static void Cull(Camera camera, ReflectionProbeCullResults results)
        {
            s_Instance.Cull(camera, results);
        }

        public static void SetProbeBoundsDirty(PlanarReflectionProbe planarProbe)
        {
            s_Instance.SetProbeBoundsDirty(planarProbe);
        }
    }
}
