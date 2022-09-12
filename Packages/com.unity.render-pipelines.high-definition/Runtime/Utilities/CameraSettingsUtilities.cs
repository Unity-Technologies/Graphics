using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Utilities for <see cref="CameraSettings"/>.</summary>
    public static class CameraSettingsUtilities
    {
        /// <summary>Applies <paramref name="settings"/> to <paramref name="cam"/>.</summary>
        /// <param name="cam">Camera to update.</param>
        /// <param name="settings">Settings to apply.</param>
        public static void ApplySettings(this Camera cam, CameraSettings settings)
        {
            var add = cam.GetComponent<HDAdditionalCameraData>()
                ?? cam.gameObject.AddComponent<HDAdditionalCameraData>();

            // FrameSettings
            add.defaultFrameSettings = settings.defaultFrameSettings;
            add.renderingPathCustomFrameSettings = settings.renderingPathCustomFrameSettings;
            add.renderingPathCustomFrameSettingsOverrideMask = settings.renderingPathCustomFrameSettingsOverrideMask;
            // Frustum
            cam.nearClipPlane = settings.frustum.nearClipPlane;
            cam.farClipPlane = settings.frustum.farClipPlane;
            cam.fieldOfView = settings.frustum.fieldOfView;
            cam.aspect = settings.frustum.aspect;
            cam.projectionMatrix = settings.frustum.GetUsedProjectionMatrix();
            // Culling
            cam.useOcclusionCulling = settings.culling.useOcclusionCulling;
            cam.cullingMask = settings.culling.cullingMask;
            cam.overrideSceneCullingMask = settings.culling.sceneCullingMaskOverride;
            // Buffer clearing
            add.clearColorMode = settings.bufferClearing.clearColorMode;
            add.backgroundColorHDR = settings.bufferClearing.backgroundColorHDR;
            add.clearDepth = settings.bufferClearing.clearDepth;
            // Volumes
            add.volumeLayerMask = settings.volumes.layerMask;
            add.volumeAnchorOverride = settings.volumes.anchorOverride;
            // HD Specific
            add.customRenderingSettings = settings.customRenderingSettings;
            add.flipYMode = settings.flipYMode;
            add.invertFaceCulling = settings.invertFaceCulling;
            add.probeCustomFixedExposure = settings.probeRangeCompressionFactor;
        }

        /// <summary>Applies <paramref name="settings"/> to <paramref name="cam"/>.</summary>
        /// <param name="cam">Camera to update.</param>
        /// <param name="settings">Settings to apply.</param>
        public static void ApplySettings(this Camera cam, CameraPositionSettings settings)
        {
            // Position
            cam.transform.SetPositionAndRotation(settings.position, settings.rotation);
            cam.worldToCameraMatrix = settings.GetUsedWorldToCameraMatrix();
        }
    }
}
