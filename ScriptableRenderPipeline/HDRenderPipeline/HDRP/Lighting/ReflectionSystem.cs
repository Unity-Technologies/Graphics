using UnityEngine.Experimental.Rendering.HDPipeline.Internal;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static class ReflectionSystem
    {
        static ReflectionSystemInternal s_Instance = new ReflectionSystemInternal(ReflectionSystemParameters.Default, null);

        static Camera s_RenderCamera = null;
        static HDAdditionalCameraData s_RenderCameraData;

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

        public static void RequestRender(PlanarReflectionProbe probe)
        {
            s_Instance.RequestRender(probe);
        }

        public static void Render(PlanarReflectionProbe probe, RenderTexture target)
        {
            var renderCamera = GetRenderCamera(probe);
            renderCamera.targetTexture = target;
        }

        static Camera GetRenderCamera(PlanarReflectionProbe probe)
        {
            if (s_RenderCamera == null)
            {
                s_RenderCamera = new GameObject("Probe Render Camera").
                    AddComponent<Camera>();
                s_RenderCameraData = s_RenderCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            }

            probe.frameSettings.CopyTo(s_RenderCameraData.GetFrameSettings());

            return s_RenderCamera;
        }
    }
}
