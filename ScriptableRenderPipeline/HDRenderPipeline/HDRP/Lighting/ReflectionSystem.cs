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

        public static void RequestRealtimeRender(PlanarReflectionProbe probe)
        {
            s_Instance.RequestRealtimeRender(probe);
        }

        public static void RenderAllRealtimeProbes()
        {
            s_Instance.RenderAllRealtimeProbes();
        }

        public static RenderTexture NewRenderTarget(PlanarReflectionProbe probe)
        {
            return s_Instance.NewRenderTarget(probe);
        }

        public static void Render(PlanarReflectionProbe probe, RenderTexture target)
        {
            s_Instance.Render(probe, target);
        }

        public static float GetCaptureCameraFOVFor(PlanarReflectionProbe probe)
        {
            return s_Instance.GetCaptureCameraFOVFor(probe);
        }

        public static void PrepareCull(Camera camera, ReflectionProbeCullResults results)
        {
            s_Instance.PrepareCull(camera, results);
        }
    }
}
