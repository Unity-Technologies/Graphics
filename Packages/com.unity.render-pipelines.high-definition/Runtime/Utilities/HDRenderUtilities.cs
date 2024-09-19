using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Various utilities to perform rendering with HDRP
    /// </summary>
    public static partial class HDRenderUtilities
    {
        /// <summary>Perform a rendering into <paramref name="target"/>.</summary>
        /// <example>
        /// How to perform rendering into a 2D render target:
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.Rendering;
        /// using UnityEngine.Rendering.HighDefinition;
        /// using UnityEngine.Experimental.Rendering;
        /// 
        /// class StandardRenderingExample
        /// {
        ///    public void Render()
        ///    {
        ///        // Copy default settings and adjust them for the custom rendering.
        ///        var cameraSettings = CameraSettings.defaultCameraSettingsNonAlloc;
        ///        cameraSettings.frustum.fieldOfView = 60.0f;
        ///        var cameraPosition = CameraPositionSettings.NewDefault();
        ///        cameraPosition.position = new Vector3(0, 1, 0);
        ///
        ///        // Create the 2D render target
        ///        var rt = new RenderTexture(128, 128, 1, GraphicsFormat.B8G8R8A8_UNorm);
        ///
        ///        // Perform the custom rendering into the render target
        ///        HDRenderUtilities.Render(cameraSettings, cameraPosition, rt);
        ///
        ///        // Implement the custom render target processing.
        ///
        ///        // Release the render target when the processing is done, RenderTexture variables are not garbage collected like normal managed types.
        ///        rt.Release();
        ///    }
        /// }
        /// </code>
        ///
        /// How to perform a cubemap rendering:
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.Rendering;
        /// using UnityEngine.Rendering.HighDefinition;
        /// using UnityEngine.Experimental.Rendering;
        /// 
        /// class CubemapRenderExample
        /// {
        ///    public void Render()
        ///    {
        ///        // Copy the default settings and adjust them for the custom rendering.
        ///        // Frustum settings from cameraSettings are ignored because the render target is a cubemap.
        ///        var cameraSettings = CameraSettings.defaultCameraSettingsNonAlloc;
        ///        var cameraPosition = CameraPositionSettings.NewDefault();
        ///        cameraPosition.position = new Vector3(0, 1, 0);
        ///
        ///        // Create the cubemap render target
        ///        var rt = new RenderTexture(128, 128, 1, GraphicsFormat.B8G8R8A8_UNorm)
        ///        {
        ///            dimension = TextureDimension.Cube
        ///        };
        ///
        ///        // Perform the custom rendering into the cubemap
        ///        HDRenderUtilities.Render(cameraSettings, cameraPosition, rt);
        ///
        ///        // Implement the custom render target processing.
        ///
        ///        // Release the render target when the processing is done, RenderTexture variables are not garbage collected like normal managed types.
        ///        rt.Release();
        ///    }
        /// }
        /// </code>
        /// </example>
        /// <param name="settings">Settings for the camera.</param>
        /// <param name="position">Position for the camera.</param>
        /// <param name="target">Target to render to.</param>
        /// <param name="staticFlags">Only used in the Editor for cubemaps.
        /// This is a bitmask of <see cref="UnityEditor.StaticEditorFlags"/>, only objects with these flags are rendered.
        /// </param>
        public static void Render(
            CameraSettings settings,
            CameraPositionSettings position,
            Texture target,
            uint staticFlags = 0
        )
        {
            // Argument checking
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var rtTarget = target as RenderTexture;
            var cubeTarget = target as Cubemap;
            switch (target.dimension)
            {
                case TextureDimension.Tex2D:
                    if (rtTarget == null)
                        throw new ArgumentException("'target' must be a RenderTexture when rendering into a 2D texture");
                    break;
                case TextureDimension.Cube:
                    break;
                default:
                    throw new ArgumentException("Rendering into a target of dimension "
                        + $"{target.dimension} is not supported");
            }

            var camera = NewRenderingCamera();
            try
            {
                camera.ApplySettings(settings);
                camera.ApplySettings(position);

                switch (target.dimension)
                {
                    case TextureDimension.Tex2D:
                    {
#if DEBUG
                        Debug.LogWarning(
                            "A static flags bitmask was provided but this is ignored when rendering into a Tex2D"
                        );
#endif
                        Assert.IsNotNull(rtTarget);
                        camera.targetTexture = rtTarget;
                        camera.Render();
                        camera.targetTexture = null;
                        target.IncrementUpdateCount();
                        break;
                    }
                    case TextureDimension.Cube:
                    {
                        Assert.IsTrue(rtTarget != null || cubeTarget != null);

                        var canHandleStaticFlags = false;
#if UNITY_EDITOR
                        canHandleStaticFlags = true;
#endif
                        // ReSharper disable ConditionIsAlwaysTrueOrFalse
                        if (canHandleStaticFlags && staticFlags != 0)
                        // ReSharper restore ConditionIsAlwaysTrueOrFalse
                        {
#if UNITY_EDITOR
                            UnityEditor.Rendering.EditorCameraUtils.RenderToCubemap(
                                camera,
                                rtTarget,
                                -1,
                                (UnityEditor.StaticEditorFlags)staticFlags
                            );
#endif
                        }
                        else
                        {
                            // ReSharper disable ConditionIsAlwaysTrueOrFalse
                            if (!canHandleStaticFlags && staticFlags != 0)
                            // ReSharper restore ConditionIsAlwaysTrueOrFalse
                            {
                                Debug.LogWarning(
                                    "A static flags bitmask was provided but this is ignored in player builds"
                                );
                            }

                            if (rtTarget != null)
                                camera.RenderToCubemap(rtTarget);
                            if (cubeTarget != null)
                                camera.RenderToCubemap(cubeTarget);
                        }

                        target.IncrementUpdateCount();
                        break;
                    }
                }
            }
            finally
            {
                CoreUtils.Destroy(camera.gameObject);
            }
        }

        /// <summary>
        /// Performs a rendering of a probe.
        /// </summary>
        /// <param name="settings">The probe settings to use.</param>
        /// <param name="position">The probe position to use.</param>
        /// <param name="target">The texture to render into.</param>
        /// <param name="forceFlipY">Whether to force Y axis flipping.</param>
        /// <param name="forceInvertBackfaceCulling">Whether to force the backface culling inversion.</param>
        /// <param name="staticFlags">The static flags filters to use.</param>
        /// <param name="referenceFieldOfView">The reference field of view.</param>
        /// <param name="referenceAspect">The reference aspect.</param>
        public static void Render(
            ProbeSettings settings,
            ProbeCapturePositionSettings position,
            Texture target,
            bool forceFlipY = false,
            bool forceInvertBackfaceCulling = false,
            uint staticFlags = 0,
            float referenceFieldOfView = 90,
            float referenceAspect = 1
        )
        {
            Render(
                settings, position, target,
                out _, out _,
                forceFlipY,
                forceInvertBackfaceCulling,
                staticFlags,
                referenceFieldOfView,
                referenceAspect
            );
        }

        static readonly Vector3[] s_GenerateRenderingSettingsFor_Rotations =
        {
            new Vector3(0, 90, 0),
            new Vector3(0, 270, 0),
            new Vector3(270, 0, 0),
            new Vector3(90, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, 180, 0),
        };
        /// <summary>
        /// Generate the camera render settings and camera position to use to render a probe.
        /// </summary>
        /// <param name="settings">The probe settings to use.</param>
        /// <param name="position">The probe position to use.</param>
        /// <param name="cameras">Will receives the camera settings.</param>
        /// <param name="cameraPositions">Will receives the camera position settings.</param>
        /// <param name="cameraCubeFaces">Will receive the camera cube face settings.</param>
        /// <param name="overrideSceneCullingMask">Override of the scene culling mask.</param>
        /// <param name="renderSteps">The rendering steps to perform for this probe.</param>
        /// <param name="forceFlipY">Whether to force the Y axis flipping.</param>
        /// <param name="referenceFieldOfView">The reference field of view.</param>
        /// <param name="referenceAspect">The reference aspect ratio.</param>
        public static void GenerateRenderingSettingsFor(
            ProbeSettings settings, ProbeCapturePositionSettings position,
            List<CameraSettings> cameras, List<CameraPositionSettings> cameraPositions, List<CubemapFace> cameraCubeFaces,
            ulong overrideSceneCullingMask,
            ProbeRenderSteps renderSteps,
            bool forceFlipY = false,
            float referenceFieldOfView = 90,
            float referenceAspect = 1
        )
        {
            // Copy settings
            ComputeCameraSettingsFromProbeSettings(
                settings, position,
                out var cameraSettings, out var cameraPositionSettings, overrideSceneCullingMask,
                referenceFieldOfView, referenceAspect
            );

            if (forceFlipY)
                cameraSettings.flipYMode = HDAdditionalCameraData.FlipYMode.ForceFlipY;

            switch (settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                {
                    cameras.Add(cameraSettings);
                    cameraPositions.Add(cameraPositionSettings);
                    cameraCubeFaces.Add(CubemapFace.Unknown);
                    break;
                }
                case ProbeSettings.ProbeType.ReflectionProbe:
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        CubemapFace face = (CubemapFace)i;
                        if (!renderSteps.HasCubeFace(face))
                            continue;
                        var cameraPositionCopy = cameraPositionSettings;
                        cameraPositionCopy.rotation = cameraPositionCopy.rotation * Quaternion.Euler(
                            s_GenerateRenderingSettingsFor_Rotations[i]
                        );
                        cameras.Add(cameraSettings);
                        cameraPositions.Add(cameraPositionCopy);
                        cameraCubeFaces.Add(face);
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Compute the camera settings from the probe settings
        /// </summary>
        /// <param name="settings">The probe settings.</param>
        /// <param name="position">The probe position.</param>
        /// <param name="cameraSettings">The produced camera settings.</param>
        /// <param name="cameraPositionSettings">The produced camera position.</param>
        /// <param name="overrideSceneCullingMask">Override of the scene culling mask.</param>
        /// <param name="referenceFieldOfView">The reference field of view.</param>
        /// <param name="referenceAspect">The reference aspect ratio.</param>
        public static void ComputeCameraSettingsFromProbeSettings(
            ProbeSettings settings,
            ProbeCapturePositionSettings position,
            out CameraSettings cameraSettings,
            out CameraPositionSettings cameraPositionSettings,
            ulong overrideSceneCullingMask,
            float referenceFieldOfView = 90,
            float referenceAspect = 1
        )
        {
            // Copy settings
            cameraSettings = settings.cameraSettings;
            cameraPositionSettings = CameraPositionSettings.NewDefault();

            // Update settings
            ProbeSettingsUtilities.ApplySettings(
                ref settings, ref position,
                ref cameraSettings, ref cameraPositionSettings,
                referenceFieldOfView,
                referenceAspect
            );

            cameraSettings.culling.sceneCullingMaskOverride = overrideSceneCullingMask;
        }

        /// <summary>
        /// Render a probe
        /// </summary>
        /// <param name="settings">The probe settings to use</param>
        /// <param name="position">The probe position to use</param>
        /// <param name="target">The target texture.</param>
        /// <param name="cameraSettings">The camera settings used during the rendering</param>
        /// <param name="cameraPositionSettings">The camera position settings used during the rendering.</param>
        /// <param name="forceFlipY">Whether to force the Y axis flipping.</param>
        /// <param name="forceInvertBackfaceCulling">Whether to force the backface culling inversion.</param>
        /// <param name="staticFlags">The static flag to use during the rendering.</param>
        /// <param name="referenceFieldOfView">The reference field of view.</param>
        /// <param name="referenceAspect">The reference aspect ratio.</param>
        public static void Render(
            ProbeSettings settings,
            ProbeCapturePositionSettings position,
            Texture target,
            out CameraSettings cameraSettings,
            out CameraPositionSettings cameraPositionSettings,
            bool forceFlipY = false,
            bool forceInvertBackfaceCulling = false,
            uint staticFlags = 0,
            float referenceFieldOfView = 90,
            float referenceAspect = 1
        )
        {
            // Copy settings
            ComputeCameraSettingsFromProbeSettings(
                settings, position,
                out cameraSettings, out cameraPositionSettings, 0,
                referenceFieldOfView, referenceAspect
            );

            if (forceFlipY)
                cameraSettings.flipYMode = HDAdditionalCameraData.FlipYMode.ForceFlipY;
            if (forceInvertBackfaceCulling)
                cameraSettings.invertFaceCulling = true;

            // Perform rendering
            Render(cameraSettings, cameraPositionSettings, target, staticFlags);
        }

        /// <summary>
        /// Create the texture used as target for a realtime reflection probe.
        /// </summary>
        /// <param name="cubemapSize">The cubemap size.</param>
        /// <returns>The texture to use as reflection probe target.</returns>
        [Obsolete("Use CreateReflectionProbeRenderTarget with explicit format instead", true)]
        public static RenderTexture CreateReflectionProbeRenderTarget(int cubemapSize)
        {
            RenderTexture rt = new RenderTexture(cubemapSize, cubemapSize, 0, GraphicsFormat.R16G16B16A16_SFloat)
            {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = true,
                autoGenerateMips = false
            };
            rt.Create();
            return rt;
        }

        /// <summary>
        /// Create the texture used as target for a realtime reflection probe.
        /// </summary>
        /// <param name="cubemapSize">The cubemap size.</param>
        /// <param name="format">The cubemap format. It must match the format set in the asset.</param>
        /// <returns>The texture to use as reflection probe target.</returns>
        public static RenderTexture CreateReflectionProbeRenderTarget(int cubemapSize, GraphicsFormat format)
        {
            RenderTexture rt = new RenderTexture(cubemapSize, cubemapSize, 0, format)
            {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = true,
                autoGenerateMips = false,
                depth = 0
            };
            rt.Create();
            return rt;
        }

        /// <summary>
        /// Create the texture used as target for a realtime planar reflection probe.
        /// </summary>
        /// <param name="planarSize">The size of the texture</param>
        /// <param name="format">The planar probe format. It must match the format set in the asset.</param>
        /// <returns>The texture used as planar reflection probe target</returns>
        public static RenderTexture CreatePlanarProbeRenderTarget(int planarSize, GraphicsFormat format)
        {
            RenderTexture rt = new RenderTexture(planarSize, planarSize, 0, format)
            {
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true,
                useMipMap = true,
                autoGenerateMips = false,
                depth = 0
            };
            rt.Create();
            return rt;
        }

        /// <summary>
        /// Create the depth texture used as target for a realtime planar reflection probe.
        /// </summary>
        /// <param name="planarSize">The size of the texture</param>
        /// <returns>The texture used as planar reflection probe target</returns>
        public static RenderTexture CreatePlanarProbeDepthRenderTarget(int planarSize)
        {
            RenderTexture rt = new RenderTexture(planarSize, planarSize, 0, GraphicsFormat.R32_SFloat)
            {
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true,
                useMipMap = true,
                autoGenerateMips = false
            };
            rt.Create();
            return rt;
        }

        /// <summary>
        /// Create the texture target for a baked reflection probe.
        /// </summary>
        /// <param name="cubemapSize">The size of the cubemap.</param>
        /// <returns>The target cubemap.</returns>
        public static Cubemap CreateReflectionProbeTarget(int cubemapSize)
        {
            return new Cubemap(cubemapSize, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
        }

        static Camera NewRenderingCamera()
        {
            var go = new GameObject("__Render Camera");
            var camera = go.AddComponent<Camera>();
            camera.cameraType = CameraType.Reflection;
            go.AddComponent<HDAdditionalCameraData>();

            return camera;
        }

        static void FixSettings(
            Texture target,
            ref ProbeSettings settings, ref ProbeCapturePositionSettings position,
            ref CameraSettings cameraSettings, ref CameraPositionSettings cameraPositionSettings
        )
        {
            // Fix a specific case
            // When rendering into a cubemap with Camera.RenderToCubemap
            // Unity will flip the image during the read back before writing into the cubemap
            // But in the end, the cubemap is flipped
            // So we force in the HDRP to flip the last blit so we have the proper flipping.
            RenderTexture rt = null;
            if ((rt = target as RenderTexture) != null
                && rt.dimension == TextureDimension.Cube
                && settings.type == ProbeSettings.ProbeType.ReflectionProbe
                && SystemInfo.graphicsUVStartsAtTop)
                cameraSettings.flipYMode = HDAdditionalCameraData.FlipYMode.ForceFlipY;
        }
    }
}
