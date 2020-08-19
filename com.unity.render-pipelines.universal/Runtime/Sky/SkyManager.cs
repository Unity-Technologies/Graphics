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

            if (cameraData.lightingSky.IsValid())
            {
                SetupAmbientProbe(ref cameraData);
            }
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

        internal static void SetupAmbientProbe(ref CameraData cameraData)
        {
            // Working around GI current system
            // When using baked lighting, setting up the ambient probe should be sufficient => We only need to update RenderSettings.ambientProbe with either the static or visual sky ambient probe
            // When using real time GI. Enlighten will pull sky information from Skybox material. So in order for dynamic GI to work, we update the skybox material texture and then set the ambient mode to SkyBox
            // Problem: We can't check at runtime if realtime GI is enabled so we need to take extra care (see useRealtimeGI usage below)

            // Order is important!
            RenderSettings.ambientMode = AmbientMode.Custom; // Needed to specify ourselves the ambient probe (this will update internal ambient probe data passed to shaders)
            RenderSettings.ambientProbe = cameraData.lightingSky.skyRenderer.GetAmbientProbe(ref cameraData);

            // TODO: Skybox material for realtime GI
        }
    }

    public static class SkyShaderConstants
    {
        public static readonly int _SkyIntensity = Shader.PropertyToID("_SkyIntensity");
        public static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");
    }
}
