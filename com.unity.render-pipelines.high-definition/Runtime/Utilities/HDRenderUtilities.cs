using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>
    /// Various utilities to perform rendering with HDRP
    /// </summary>
    public static partial class HDRenderUtilities
    {
        /// <summary>Perform a rendering into <paramref name="target"/>.</summary>
        /// <example>
        /// How to perform standard rendering:
        /// <code>
        /// class StandardRenderingExample
        /// {
        ///     public void Render()
        ///     {
        ///         // Copy default settings
        ///         var settings = CameraRenderSettings.Default;
        ///         // Adapt default settings to our custom usage
        ///         settings.position.position = new Vector3(0, 1, 0);
        ///         settings.camera.frustum.fieldOfView = 60.0f;
        ///         // Get our render target
        ///         var rt = new RenderTexture(128, 128, 1, GraphicsFormat.B8G8R8A8_SNorm);
        ///         HDRenderUtilities.Render(settings, rt);
        ///         // Do something with rt
        ///         rt.Release();
        ///     }
        /// }
        /// </code>
        ///
        /// How to perform a cubemap rendering:
        /// <code>
        /// class CubemapRenderExample
        /// {
        ///     public void Render()
        ///     {
        ///         // Copy default settings
        ///         var settings = CameraRenderSettings.Default;
        ///         // Adapt default settings to our custom usage
        ///         settings.position.position = new Vector3(0, 1, 0);
        ///         settings.camera.physical.iso = 800.0f;
        ///         // Frustum settings are ignored and driven by the cubemap rendering
        ///         // Get our render target
        ///         var rt = new RenderTexture(128, 128, 1, GraphicsFormat.B8G8R8A8_SNorm)
        ///         {
        ///             dimension = TextureDimension.Cube
        ///         };
        ///         // The TextureDimension is detected and the renderer will perform a cubemap rendering.
        ///         HDRenderUtilities.Render(settings, rt);
        ///         // Do something with rt
        ///         rt.Release();
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="settings">Settings for the camera.</param>
        /// <param name="position">Position for the camera.</param>
        /// <param name="target">Target to render to.</param>
        /// <param name="staticFlags">Only used in the Editor fo cubemaps.
        /// This is bitmask of <see cref="UnityEditor.StaticEditorFlags"/> only objects with these flags will be rendered
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

        public static void Render(
            ProbeSettings settings,
            ProbeCapturePositionSettings position,
            Texture target,
            bool forceFlipY = false,
            bool forceInvertBackfaceCulling = false,
            uint staticFlags = 0
        )
        {
            Render(
                settings, position, target,
                out CameraSettings cameraSettings, out CameraPositionSettings cameraPosition,
                forceFlipY: forceFlipY,
                forceInvertBackfaceCulling: forceInvertBackfaceCulling,
                staticFlags: staticFlags
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
        public static void GenerateRenderingSettingsFor(
            ProbeSettings settings, ProbeCapturePositionSettings position,
            List<CameraSettings> cameras, List<CameraPositionSettings> cameraPositions,
            bool forceFlipY = false
        )
        {
            // Copy settings
            ComputeCameraSettingsFromProbeSettings(
                settings, position,
                out CameraSettings cameraSettings, out CameraPositionSettings cameraPositionSettings
            );

            if (forceFlipY)
                cameraSettings.flipYMode = HDAdditionalCameraData.FlipYMode.ForceFlipY;

            switch (settings.type)
            {
                case ProbeSettings.ProbeType.PlanarProbe:
                    {
                        cameras.Add(cameraSettings);
                        cameraPositions.Add(cameraPositionSettings);
                        break;
                    }
                case ProbeSettings.ProbeType.ReflectionProbe:
                    {
                        for (int i = 0; i < 6; ++i)
                        {
                            var cameraPositionCopy = cameraPositionSettings;
                            cameraPositionCopy.rotation = cameraPositionCopy.rotation * Quaternion.Euler(
                                s_GenerateRenderingSettingsFor_Rotations[i]
                            );
                            cameras.Add(cameraSettings);
                            cameraPositions.Add(cameraPositionCopy);
                        }
                        break;
                    }
            }
        }

        public static void ComputeCameraSettingsFromProbeSettings(
            ProbeSettings settings,
            ProbeCapturePositionSettings position,
            out CameraSettings cameraSettings,
            out CameraPositionSettings cameraPositionSettings
        )
        {
            // Copy settings
            cameraSettings = settings.camera;
            cameraPositionSettings = CameraPositionSettings.@default;

            // Update settings
            ProbeSettingsUtilities.ApplySettings(
                ref settings, ref position,
                ref cameraSettings, ref cameraPositionSettings
            );
        }

        public static void Render(
            ProbeSettings settings,
            ProbeCapturePositionSettings position,
            Texture target,
            out CameraSettings cameraSettings,
            out CameraPositionSettings cameraPositionSettings,
            bool forceFlipY = false,
            bool forceInvertBackfaceCulling = false,
            uint staticFlags = 0
        )
        {
            // Copy settings
            ComputeCameraSettingsFromProbeSettings(
                settings, position,
                out cameraSettings, out cameraPositionSettings
            );

            if (forceFlipY)
                cameraSettings.flipYMode = HDAdditionalCameraData.FlipYMode.ForceFlipY;
            if (forceInvertBackfaceCulling)
                cameraSettings.invertFaceCulling = true;

            // Perform rendering
            Render(cameraSettings, cameraPositionSettings, target, staticFlags);
        }

        public static RenderTexture CreateReflectionProbeRenderTarget(int cubemapSize)
        {
            return new RenderTexture(cubemapSize, cubemapSize, 1, GraphicsFormat.R16G16B16A16_SFloat)
            {
                dimension = TextureDimension.Cube,
                enableRandomWrite = true,
                useMipMap = true,
                autoGenerateMips = false
            };
        }

        public static RenderTexture CreatePlanarProbeRenderTarget(int planarSize)
        {
            return new RenderTexture(planarSize, planarSize, 1, GraphicsFormat.R16G16B16A16_SFloat)
            {
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true,
                useMipMap = true,
                autoGenerateMips = false
            };
        }

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
