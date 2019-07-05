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
            MirrorReferenceTransformWithProbePlane
        }

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
            ref CameraPositionSettings cameraPosition,              // InOut parameter
            float referenceFieldOfView = 90
        )
        {
            cameraSettings = settings.camera;
            // Compute the modes for each probe type
            PositionMode positionMode;
            bool useReferenceTransformAsNearClipPlane;
            switch (settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                    positionMode = PositionMode.MirrorReferenceTransformWithProbePlane;
                    useReferenceTransformAsNearClipPlane = true;
                    ApplyPlanarFrustumHandling(
                        ref settings, ref probePosition,
                        ref cameraSettings, ref cameraPosition,
                        referenceFieldOfView
                    );
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

                        // In case of probe baking, 99% of the time, orientation of the cubemap doesn't matters
                        //   so, we build one without any rotation, thus we don't have to change the basis
                        //   during sampling the cubemap.
                        if (settings.type == ProbeSettings.ProbeType.ReflectionProbe)
                            cameraPosition.rotation = Quaternion.identity;
                        break;
                    }
                case PositionMode.MirrorReferenceTransformWithProbePlane:
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
            switch (settings.mode)
            {
                default:
                case ProbeSettings.Mode.Realtime:
                    cameraSettings.defaultFrameSettings = FrameSettingsRenderType.RealtimeReflection;
                    break;
                case ProbeSettings.Mode.Baked:
                case ProbeSettings.Mode.Custom:
                    cameraSettings.defaultFrameSettings = FrameSettingsRenderType.CustomOrBakedReflection;
                    break;
            }

            switch (settings.type)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                    cameraSettings.customRenderingSettings = true;
                    // Disable specular lighting for reflection probes, they must not have view dependent information when baking
                    cameraSettings.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.SpecularLighting, false);
                    cameraSettings.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SpecularLighting] = true;
                    break;
            }
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
            var reflectionMatrix = GeometryUtils.CalculateReflectionMatrix(mirrorPosition, mirrorForward);

            var worldToCameraRHS = GeometryUtils.CalculateWorldToCameraMatrixRHS(
                probePosition.referencePosition,

                // TODO: The capture camera should look at a better direction to only capture texels that
                //   will actually be sampled.
                //   The position it should look at is the center of the visible influence volume of the probe.
                //   (visible influence volume: the intersection of the frustum with the probe's influence volume).
                //   But this is not trivial to get.
                //   So currently, only look in the mirrored direction of the reference. This will capture
                //   more pixels than we want with a lesser resolution, but still work for most cases.

                // Note: looking at the center of the influence volume don't work in all cases (see case 1157921)
                probePosition.referenceRotation
            );
            cameraPosition.worldToCameraMatrix = worldToCameraRHS * reflectionMatrix;
            // We must invert the culling because we performed a plane reflection
            cameraSettings.invertFaceCulling = true;

            // Calculate capture position and rotation
            cameraPosition.position = reflectionMatrix.MultiplyPoint(probePosition.referencePosition);
            var forward = reflectionMatrix.MultiplyVector(probePosition.referenceRotation * Vector3.forward);
            var up = reflectionMatrix.MultiplyVector(probePosition.referenceRotation * Vector3.up);
            cameraPosition.rotation = Quaternion.LookRotation(forward, up);
        }

        internal static void ApplyPlanarFrustumHandling(
            ref ProbeSettings settings,                             // In Parameter
            ref ProbeCapturePositionSettings probePosition,         // In parameter
            ref CameraSettings cameraSettings,                      // InOut parameter
            ref CameraPositionSettings cameraPosition,              // InOut parameter
            float referenceFieldOfView
        )
        {
            const float k_MaxFieldOfView = 170;

            var proxyMatrix = Matrix4x4.TRS(probePosition.proxyPosition, probePosition.proxyRotation, Vector3.one);
            var mirrorPosition = proxyMatrix.MultiplyPoint(settings.proxySettings.mirrorPositionProxySpace);

            switch (settings.frustum.fieldOfViewMode)
            {
                case ProbeSettings.Frustum.FOVMode.Fixed:
                    cameraSettings.frustum.fieldOfView = settings.frustum.fixedValue;
                    break;
                case ProbeSettings.Frustum.FOVMode.Viewer:
                    cameraSettings.frustum.fieldOfView = Mathf.Min(
                        referenceFieldOfView * settings.frustum.viewerScale,
                        k_MaxFieldOfView
                    );
                    break;
                case ProbeSettings.Frustum.FOVMode.Automatic:
                    // Dynamic FOV tries to adapt the FOV to have maximum usage of the target render texture
                    //     (A lot of pixel can be discarded in the render texture). This way we can have a greater
                    //     resolution for the planar with the same cost.
                    cameraSettings.frustum.fieldOfView = Mathf.Min(
                            settings.influence.ComputeFOVAt(
                            probePosition.referencePosition, mirrorPosition, probePosition.influenceToWorld
                        ) * settings.frustum.automaticScale,
                            k_MaxFieldOfView
                    );
                    break;
            }
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
