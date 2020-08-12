using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public class SkyManager
    {
        private static Dictionary<Camera, SkyUpdateContext> visualSkiesCache = new Dictionary<Camera, SkyUpdateContext>();

        public static void UpdateCurrentSkySettings(ref CameraData cameraData)
        {
            // TODO Editor preview camera

            var volumeStack = VolumeManager.instance.stack;

            cameraData.skyAmbientMode = volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;

            if (!visualSkiesCache.ContainsKey(cameraData.camera))
            {
                visualSkiesCache[cameraData.camera] = new SkyUpdateContext();
            }
            cameraData.visualSky = visualSkiesCache[cameraData.camera];
            cameraData.visualSky.skySettings = GetSkySettings(volumeStack);

            // TODO Lighting override
            cameraData.lightingSky = cameraData.visualSky;
        }

        private static SkySettings GetSkySettings(VolumeStack stack)
        {
            var visualEnvironmet = stack.GetComponent<VisualEnvironment>();
            int skyID = visualEnvironmet.skyType.value;

            Type skyType;
            if (SkyTypesCatalog.skyTypesDict.TryGetValue(skyID, out skyType))
            {
                return stack.GetComponent(skyType) as SkySettings;
            }

            return null;
        }

        public static void PrerenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            var skyContext = cameraData.visualSky;
            if (skyContext.IsValid())
            {
                // TODO
                skyContext.skyRenderer.PrerenderSky(ref cameraData, cmd);
            }
        }

        public static void RenderSky(ref CameraData cameraData, CommandBuffer cmd)
        {
            var skyContext = cameraData.visualSky;
            if (skyContext.IsValid())
            {
                // TODO
                skyContext.skyRenderer.RenderSky(ref cameraData, cmd);
            }
        }
    }

    public static class SkyShaderConstants
    {
        public static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");
    }
}
