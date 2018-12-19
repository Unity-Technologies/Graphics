using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Utilities for <see cref="ProbeSettings"/></summary>
    public static class ProbeSettingsUtilities
    {
        internal enum PositionMode
        {
            UseProbeTransform,
            MirrorReferenceTransfromWithProbePlane
        }

        // This is viable to use a static variable here because ApplySettings() must be called only on main thread.
        static FrameSettings s_ApplySettings_TMP = new FrameSettings();
        /// <summary>
        /// Apply <paramref name="settings"/> and <paramref name="probePosition"/> to
        /// <paramref name="cameraPosition"/> and <paramref name="cameraSettings"/>.
        /// </summary>
        /// <param name="settings">Settings to apply. (Read only)</param>
        /// <param name="probePosition">Position to apply. (Read only)</param>
        /// <param name="cameraSettings">Settings to update.</param>
        /// <param name="cameraPosition">Position to update.</param>
        public static void ApplySettings(
            ref ProbeSettings settings,                             // In Parameter
            ref ProbeCapturePositionSettings probePosition,         // In parameter
            ref CameraSettings cameraSettings,                      // InOut parameter
            ref CameraPositionSettings cameraPosition               // InOut parameter
        )
        {
            cameraSettings = settings.camera;
            // Compute the modes for each probe type
            PositionMode positionMode;
            bool useReferenceTransformAsNearClipPlane;
            switch (settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                    positionMode = PositionMode.MirrorReferenceTransfromWithProbePlane;
                    useReferenceTransformAsNearClipPlane = true;
                    break;
                case ProbeSettings.ProbeType.ReflectionProbe:
                    positionMode = PositionMode.UseProbeTransform;
                    useReferenceTransformAsNearClipPlane = false;
                    cameraSettings.frustum.mode = CameraSettings.Frustum.Mode.ComputeProjectionMatrix;
                    cameraSettings.frustum.aspect = 1;
                    cameraSettings.frustum.fieldOfView = 90;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update the position
            switch (positionMode)
            {
                case PositionMode.UseProbeTransform:
                    {
                        cameraPosition.mode = CameraPositionSettings.Mode.ComputeWorldToCameraMatrix;
                        var proxyMatrix = Matrix4x4.TRS(probePosition.proxyPosition, probePosition.proxyRotation, Vector3.one);
                        cameraPosition.position = proxyMatrix.MultiplyPoint(settings.proxySettings.capturePositionProxySpace);
                        cameraPosition.rotation = proxyMatrix.rotation * settings.proxySettings.captureRotationProxySpace;
                        break;
                    }
                case PositionMode.MirrorReferenceTransfromWithProbePlane:
                    {
                        cameraPosition.mode = CameraPositionSettings.Mode.UseWorldToCameraMatrixField;
                        ApplyMirroredReferenceTransform(
                            ref settings, ref probePosition,
                            ref cameraSettings, ref cameraPosition
                        );
                        break;
                    }
            }

            // Update the clip plane
            if (useReferenceTransformAsNearClipPlane)
            {
                ApplyObliqueNearClipPlane(
                    ref settings, ref probePosition,
                    ref cameraSettings, ref cameraPosition
                );
            }

            // Frame Settings Overrides
            var hd = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            switch (settings.mode)
            {
                default:
                case ProbeSettings.Mode.Realtime:
                    hd.asset.GetRealtimeReflectionFrameSettings().CopyTo(s_ApplySettings_TMP);
                    break;
                case ProbeSettings.Mode.Baked:
                case ProbeSettings.Mode.Custom:
                    hd.asset.GetBakedOrCustomReflectionFrameSettings().CopyTo(s_ApplySettings_TMP);
                    break;
            }
            cameraSettings.frameSettings.ApplyOverrideOn(s_ApplySettings_TMP);
            s_ApplySettings_TMP.CopyTo(cameraSettings.frameSettings);
        }

        internal static void ApplyMirroredReferenceTransform(
            ref ProbeSettings settings,                             // In Parameter
            ref ProbeCapturePositionSettings probePosition,         // In parameter
            ref CameraSettings cameraSettings,                      // InOut parameter
            ref CameraPositionSettings cameraPosition               // InOut parameter
        )
        {
            // Calculate mirror position and forward world space
            var proxyMatrix = Matrix4x4.TRS(probePosition.proxyPosition, probePosition.proxyRotation, Vector3.one);
            var mirrorPosition = proxyMatrix.MultiplyPoint(settings.proxySettings.mirrorPositionProxySpace);
            var mirrorForward = proxyMatrix.MultiplyVector(settings.proxySettings.mirrorRotationProxySpace * Vector3.forward);

            var worldToCameraRHS = GeometryUtils.CalculateWorldToCameraMatrixRHS(
                probePosition.referencePosition,
                probePosition.referenceRotation
            );
            var reflectionMatrix = GeometryUtils.CalculateReflectionMatrix(mirrorPosition, mirrorForward);
            cameraPosition.worldToCameraMatrix = worldToCameraRHS * reflectionMatrix;
            // We must invert the culling because we performed a plane reflection
            cameraSettings.invertFaceCulling = true;

            // Calculate capture position and rotation
            cameraPosition.position = reflectionMatrix.MultiplyPoint(probePosition.referencePosition);
            var forward = reflectionMatrix.MultiplyVector(probePosition.referenceRotation * Vector3.forward);
            var up = reflectionMatrix.MultiplyVector(probePosition.referenceRotation * Vector3.up);
            cameraPosition.rotation = Quaternion.LookRotation(forward, up);
        }

        internal static void ApplyObliqueNearClipPlane(
            ref ProbeSettings settings,                             // In Parameter
            ref ProbeCapturePositionSettings probePosition,         // In parameter
            ref CameraSettings cameraSettings,                      // InOut parameter
            ref CameraPositionSettings cameraPosition               // InOut parameter
        )
        {
            var proxyMatrix = Matrix4x4.TRS(probePosition.proxyPosition, probePosition.proxyRotation, Vector3.one);
            var mirrorPosition = proxyMatrix.MultiplyPoint(settings.proxySettings.mirrorPositionProxySpace);
            var mirrorForward = proxyMatrix.MultiplyVector(settings.proxySettings.mirrorRotationProxySpace * Vector3.forward);

            var clipPlaneCameraSpace = GeometryUtils.CameraSpacePlane(
                cameraPosition.worldToCameraMatrix,
                mirrorPosition,
                mirrorForward
            );
            var sourceProjection = Matrix4x4.Perspective(
                cameraSettings.frustum.fieldOfView,
                cameraSettings.frustum.aspect,
                cameraSettings.frustum.nearClipPlane,
                cameraSettings.frustum.farClipPlane
            );
            var obliqueProjection = GeometryUtils.CalculateObliqueMatrix(
                sourceProjection, clipPlaneCameraSpace
            );
            cameraSettings.frustum.mode = CameraSettings.Frustum.Mode.UseProjectionMatrixField;
            cameraSettings.frustum.projectionMatrix = obliqueProjection;
        }
    }
}
