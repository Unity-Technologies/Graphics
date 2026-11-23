using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    // The buffers BufferCapture can produce, mostly Rendering Debugger views
    static class UniversalBufferCaptureTargets
    {
        // Renderer2D's sprite shaders output the invalid mode color (red) for the material and lighting modes they
        // don't implement, and a constant world space normal. Only UniversalRenderer composites full screen debug modes.
        static readonly Type[] k_UniversalRenderer = { typeof(UniversalRenderer) };
        static readonly Type[] k_UniversalAnd2DRenderers = { typeof(UniversalRenderer), typeof(Renderer2D) };

        static readonly BufferCapture.Target[] k_Targets =
        {
            Create("color", "Final camera output, with Rendering Debugger modes disabled.", null),
            new BufferCapture.Target("object_id", "Each object in a distinct color, with a JSON file mapping colors to GameObjects (uses Scene view picking). Objects rendered by Overlay cameras in the camera's stack are not included.", false, null, isObjectId: true),
            Create("depth", "Depth buffer, remapped for display.", k_UniversalRenderer, fullScreenMode: DebugFullScreenMode.Depth, linearOutput: true),
            Create("shadow_main", "Main light shadow map atlas.", k_UniversalRenderer, fullScreenMode: DebugFullScreenMode.MainLightShadowMap, squareOutput: true, linearOutput: true, getSceneUnsupportedReason: GetMainLightShadowsUnsupportedReason),
            Create("shadow_additional", "Additional lights shadow map atlas.", k_UniversalRenderer, fullScreenMode: DebugFullScreenMode.AdditionalLightsShadowMap, squareOutput: true, linearOutput: true, getSceneUnsupportedReason: GetAdditionalLightShadowsUnsupportedReason),
            Create("albedo", "Material albedo.", k_UniversalAnd2DRenderers, materialMode: DebugMaterialMode.Albedo),
            Create("specular", "Material specular color.", k_UniversalRenderer, materialMode: DebugMaterialMode.Specular),
            Create("alpha", "Material alpha.", k_UniversalAnd2DRenderers, materialMode: DebugMaterialMode.Alpha, linearOutput: true),
            Create("smoothness", "Material smoothness.", k_UniversalRenderer, materialMode: DebugMaterialMode.Smoothness, linearOutput: true),
            Create("metallic", "Material metallic.", k_UniversalRenderer, materialMode: DebugMaterialMode.Metallic, linearOutput: true),
            Create("ambient_occlusion", "Material ambient occlusion.", k_UniversalRenderer, materialMode: DebugMaterialMode.AmbientOcclusion, linearOutput: true),
            Create("emission", "Material emission.", k_UniversalRenderer, materialMode: DebugMaterialMode.Emission),
            Create("normal", "World space normals.", k_UniversalRenderer, materialMode: DebugMaterialMode.NormalWorldSpace, linearOutput: true),
            Create("normal_tangent", "Tangent space normals.", k_UniversalAnd2DRenderers, materialMode: DebugMaterialMode.NormalTangentSpace, linearOutput: true),
            Create("lighting_complexity", "Number of lights affecting each pixel.", k_UniversalRenderer, materialMode: DebugMaterialMode.LightingComplexity),
            Create("shadow_cascades", "Main light shadow cascades in distinct colors.", k_UniversalRenderer, lightingMode: DebugLightingMode.ShadowCascades),
            Create("lighting", "Lighting only, including normal maps.", k_UniversalAnd2DRenderers, lightingMode: DebugLightingMode.LightingWithNormalMaps),
            Create("reflections", "Reflections only.", k_UniversalRenderer, lightingMode: DebugLightingMode.Reflections),
            Create("global_illumination", "Indirect irradiance.", k_UniversalRenderer, lightingMode: DebugLightingMode.GlobalIllumination),
        };

        internal static BufferCapture.Target[] all => k_Targets;

        static BufferCapture.Target Create(
            string name,
            string description,
            Type[] supportedRenderers,
            DebugFullScreenMode fullScreenMode = DebugFullScreenMode.None,
            DebugMaterialMode materialMode = DebugMaterialMode.None,
            DebugLightingMode lightingMode = DebugLightingMode.None,
            bool squareOutput = false,
            bool linearOutput = false,
            Func<Camera, string> getSceneUnsupportedReason = null)
        {
            return new BufferCapture.Target(name, description, squareOutput,
                _ => new DebugDisplayScope(fullScreenMode, materialMode, lightingMode),
                linearOutput: linearOutput,
                getUnsupportedReason: camera => GetRendererUnsupportedReason(camera, supportedRenderers) ??
                    (fullScreenMode != DebugFullScreenMode.None ? GetFullScreenModeUnsupportedReason(camera) : null) ??
                    getSceneUnsupportedReason?.Invoke(camera));
        }

        // DebugDisplayScope needs the component to make offscreen cameras composite full screen debug modes
        static string GetFullScreenModeUnsupportedReason(Camera camera)
        {
            // URP takes Scene view post-processing from the view itself, which DebugDisplayScope can't override
            if (camera.cameraType == CameraType.SceneView && !CoreUtils.ArePostProcessesEnabled(camera))
                return "Capturing this Rendering Debugger mode from the Scene view requires its Post Processing effect to be enabled and its draw mode not to be Wireframe.";

            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out _))
                return null;

            return $"Camera '{camera.name}' has no Universal Additional Camera Data component, which capturing this Rendering Debugger mode requires.";
        }

        static ScriptableRenderer GetRenderer(Camera camera)
        {
            camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData);
            return UniversalRenderPipeline.GetRenderer(camera, additionalCameraData);
        }

        // A null supportedRenderers means any renderer
        static string GetRendererUnsupportedReason(Camera camera, Type[] supportedRenderers)
        {
            if (supportedRenderers == null)
                return null;

            var rendererType = GetRenderer(camera).GetType();
            if (Array.Exists(supportedRenderers, t => t.IsAssignableFrom(rendererType)))
                return null;

            return $"The camera uses {rendererType.Name}, which doesn't support this Rendering Debugger mode.";
        }

        // Only checks settings. Without a visible shadow casting light, URP gives its empty placeholder shadow map.
        static string GetCommonShadowsUnsupportedReason(Camera camera)
        {
            if (!SystemInfo.supportsShadows)
                return "The graphics device doesn't support shadows.";

            camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData);
            if (camera.cameraType != CameraType.SceneView && additionalCameraData != null && !additionalCameraData.renderShadows)
                return $"Render Shadows is disabled on camera '{camera.name}'.";

            if (VolumeManager.instance.isInitialized)
            {
                float volumeMaxShadowDistance = GetVolumeMaxShadowDistance(camera, additionalCameraData);
                if (Mathf.Min(volumeMaxShadowDistance, camera.farClipPlane) < camera.nearClipPlane)
                    return $"The Shadows volume override's Max Shadow Distance ({volumeMaxShadowDistance}) is shorter than the near clip plane of camera '{camera.name}' ({camera.nearClipPlane}).";
            }

            if (CoreUtils.IsSceneLightingDisabled(camera))
                return "Scene lighting is disabled in the Scene view.";

            return null;
        }

        // VolumeManager.instance.stack can't be used, it holds the values of the camera that rendered last.
        // In Play mode, cameras that update volumes via scripting render with a cached stack, which this ignores.
        static float GetVolumeMaxShadowDistance(Camera camera, UniversalAdditionalCameraData additionalCameraData)
        {
            camera.GetVolumeLayerMaskAndTrigger(additionalCameraData, out LayerMask layerMask, out Transform trigger);
            var stack = VolumeManager.instance.CreateStack();
            try
            {
                VolumeManager.instance.Update(stack, trigger, layerMask);
                return UniversalRenderPipeline.GetShadowsVolume(stack).maxShadowDistance.value;
            }
            finally
            {
                VolumeManager.instance.DestroyStack(stack);
            }
        }

        static string GetMainLightShadowsUnsupportedReason(Camera camera)
        {
            var asset = UniversalRenderPipeline.asset;
            if (asset.mainLightRenderingMode != LightRenderingMode.PerPixel)
                return "The URP Asset's Main Light rendering mode isn't Per Pixel.";
            if (!asset.supportsMainLightShadows)
                return "Main light shadows are disabled in the URP Asset.";

            return GetCommonShadowsUnsupportedReason(camera);
        }

        static string GetAdditionalLightShadowsUnsupportedReason(Camera camera)
        {
            var asset = UniversalRenderPipeline.asset;
            var renderer = GetRenderer(camera) as UniversalRenderer;
            bool isForwardPlus = renderer != null && renderer.renderingModeActual == RenderingMode.ForwardPlus;
            var lightsMode = asset.additionalLightsRenderingMode;
            if (lightsMode == LightRenderingMode.PerVertex || (lightsMode != LightRenderingMode.PerPixel && !isForwardPlus))
                return "The URP Asset's Additional Lights rendering mode isn't Per Pixel.";
            if (!asset.supportsAdditionalLightShadows)
                return "Additional light shadows are disabled in the URP Asset.";

            return GetCommonShadowsUnsupportedReason(camera);
        }

        // WORKAROUND: URP has no per-request debug settings, so this overrides the global
        // Rendering Debugger state around one synchronous render request. Editor views never render a frame with it.
        sealed class DebugDisplayScope : IDisposable
        {
            readonly DebugFullScreenMode m_FullScreenMode;
            readonly int m_FullScreenSizePercent;
            readonly DebugMaterialMode m_MaterialMode;
            readonly DebugLightingMode m_LightingMode;

            public DebugDisplayScope(DebugFullScreenMode fullScreenMode, DebugMaterialMode materialMode, DebugLightingMode lightingMode)
            {
                var settings = UniversalRenderPipelineDebugDisplaySettings.Instance;
                m_FullScreenMode = settings.renderingSettings.fullScreenDebugMode;
                m_FullScreenSizePercent = settings.renderingSettings.fullScreenDebugModeOutputSizeScreenPercent;
                m_MaterialMode = settings.materialSettings.materialDebugMode;
                m_LightingMode = settings.lightingSettings.lightingDebugMode;

                settings.renderingSettings.fullScreenDebugMode = fullScreenMode;
                settings.renderingSettings.fullScreenDebugModeOutputSizeScreenPercent = 100;
                settings.materialSettings.materialDebugMode = materialMode;
                settings.lightingSettings.lightingDebugMode = lightingMode;
            }

            public void Dispose()
            {
                var settings = UniversalRenderPipelineDebugDisplaySettings.Instance;
                settings.renderingSettings.fullScreenDebugMode = m_FullScreenMode;
                settings.renderingSettings.fullScreenDebugModeOutputSizeScreenPercent = m_FullScreenSizePercent;
                settings.materialSettings.materialDebugMode = m_MaterialMode;
                settings.lightingSettings.lightingDebugMode = m_LightingMode;
            }
        }
    }
}
