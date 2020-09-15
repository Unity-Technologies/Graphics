using System.Collections.Generic;
using UnityEngine.Rendering.LookDev;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline : IDataProvider
    {
        struct LookDevDataForHDRP
        {
            public HDAdditionalCameraData additionalCameraData;
            public HDAdditionalLightData additionalLightData;
            public VisualEnvironment visualEnvironment;
            public HDRISky sky;
            public Volume volume;
#if UNITY_EDITOR
            public int currentVolumeProfileHash;
#endif
        }

#if UNITY_EDITOR
        bool UpdateVolumeProfile(Volume volume, out VisualEnvironment visualEnvironment, out HDRISky sky, ref int volumeProfileHash)
        {
            HDRenderPipelineAsset hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (hdrpAsset.defaultLookDevProfile == null)
                hdrpAsset.defaultLookDevProfile = hdrpAsset.renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile;

            int newHashCode = hdrpAsset.defaultLookDevProfile.GetHashCode();
            if (newHashCode != volumeProfileHash)
            {
                VolumeProfile oldProfile = volume.sharedProfile;

                volumeProfileHash = newHashCode;

                VolumeProfile profile = ScriptableObject.Instantiate(hdrpAsset.defaultLookDevProfile);
                profile.hideFlags = HideFlags.HideAndDontSave;
                volume.sharedProfile = profile;

                // Remove potentially existing components in the user profile.
                if (profile.TryGet(out visualEnvironment))
                    profile.Remove<VisualEnvironment>();

                if (profile.TryGet(out sky))
                    profile.Remove<HDRISky>();

                // If there was a profile before we needed to re-instantiate the new profile, we need to copy the data over for sky settings.
                if (oldProfile != null)
                {
                    if (oldProfile.TryGet(out HDRISky oldSky))
                    {
                        sky = Object.Instantiate(oldSky);
                        profile.components.Add(sky);
                    }
                    if (oldProfile.TryGet(out VisualEnvironment oldVisualEnv))
                    {
                        visualEnvironment = Object.Instantiate(oldVisualEnv);
                        profile.components.Add(visualEnvironment);
                    }

                    CoreUtils.Destroy(oldProfile);
                }
                else
                {
                    visualEnvironment = profile.Add<VisualEnvironment>();
                    visualEnvironment.skyType.Override((int)SkyType.HDRI);
                    visualEnvironment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
                    sky = profile.Add<HDRISky>();
                }

                return true;
            }
            else
            {
                visualEnvironment = null;
                sky = null;
                return false;
            }
        }
#endif

        /// <summary>
        /// This hook allows HDRP to init the scene when creating the view
        /// </summary>
        /// <param name="SRI">The StageRuntimeInterface allowing to communicate with the LookDev</param>
        void IDataProvider.FirstInitScene(StageRuntimeInterface SRI)
        {
            Camera camera = SRI.camera;
            camera.allowHDR = true;

            var additionalCameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            additionalCameraData.clearDepth = true;
            additionalCameraData.backgroundColorHDR = camera.backgroundColor;
            additionalCameraData.volumeAnchorOverride = camera.transform;
            additionalCameraData.volumeLayerMask = 1 << 31; //31 is the culling layer used in LookDev

            additionalCameraData.customRenderingSettings = true;
            additionalCameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.SSR, false);
            // LookDev cameras are enabled/disabled all the time so history is destroyed each frame.
            // In this case we know we want to keep history alive as long as the camera is.
            additionalCameraData.hasPersistentHistory = true;

            Light light = SRI.sunLight;
            HDAdditionalLightData additionalLightData = light.gameObject.AddComponent<HDAdditionalLightData>();
#if UNITY_EDITOR
            HDAdditionalLightData.InitDefaultHDAdditionalLightData(additionalLightData);
#endif
            additionalLightData.intensity = 0f;
            additionalLightData.SetShadowResolution(2048);

            GameObject volumeGO = SRI.AddGameObject(persistent: true);
            volumeGO.name = "StageVolume";
            Volume volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = float.MaxValue;
            volume.enabled = false;


#if UNITY_EDITOR
            // Make sure we invalidate the current volume when first loading a scene.
            int volumeProfileHash = -1;
            UpdateVolumeProfile(volume, out var visualEnvironment, out var sky, ref volumeProfileHash);

            SRI.SRPData = new LookDevDataForHDRP()
            {
                additionalCameraData = additionalCameraData,
                additionalLightData = additionalLightData,
                visualEnvironment = visualEnvironment,
                sky = sky,
                volume = volume,
                currentVolumeProfileHash = volumeProfileHash
            };
#else
            //remove unassigned warnings when building
            SRI.SRPData = new LookDevDataForHDRP()
            {
                additionalCameraData = null,
                additionalLightData = null,
                visualEnvironment = null,
                sky = null,
                volume = null
            };
#endif
        }

        /// <summary>
        /// This hook allows HDRP to apply the sky as it is requested by the LookDev
        /// </summary>
        /// <param name="camera">The Camera rendering in the LookDev</param>
        /// <param name="sky">The requested Sky to use</param>
        /// <param name="SRI">The StageRuntimeInterface allowing to communicate with the LookDev</param>
        void IDataProvider.UpdateSky(Camera camera, Sky sky, StageRuntimeInterface SRI)
        {
            LookDevDataForHDRP data = (LookDevDataForHDRP)SRI.SRPData;
            if (sky.cubemap == null)
            {
                data.visualEnvironment.skyType.Override((int)0); //Skytype.None do not really exist
                data.additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            }
            else
            {
                data.visualEnvironment.skyType.Override((int)SkyType.HDRI);
                data.sky.hdriSky.Override(sky.cubemap);
                data.sky.rotation.Override(sky.longitudeOffset);
                data.additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;
            }
        }

        /// <summary>
        /// This hook allows HDRP to apply some changes before the LookDev's Camera render.
        /// Should mainly be used for view isolation.
        /// </summary>
        /// <param name="SRI">The StageRuntimeInterface allowing to communicate with the LookDev</param>
        void IDataProvider.OnBeginRendering(StageRuntimeInterface SRI)
        {
            LookDevDataForHDRP data = (LookDevDataForHDRP)SRI.SRPData;
#if UNITY_EDITOR
            int currentHash = data.currentVolumeProfileHash;
            // The default volume can change in the HDRP asset so if it does we need to re-instantiate it.
            if (UpdateVolumeProfile(data.volume, out var visualEnv, out var sky, ref currentHash))
            {
                data.sky = sky;
                data.visualEnvironment = visualEnv;
                data.currentVolumeProfileHash = currentHash;
                SRI.SRPData = data;
            }
#endif
            data.volume.enabled = true;
        }

        /// <summary>
        /// This hook allows HDRP to apply some changes after the LookDev's Camera render.
        /// Should mainly be used for view isolation.
        /// </summary>
        /// <param name="SRI">The StageRuntimeInterface allowing to communicate with the LookDev</param>
        void IDataProvider.OnEndRendering(StageRuntimeInterface SRI)
        {
            LookDevDataForHDRP data = (LookDevDataForHDRP)SRI.SRPData;
            data.volume.enabled = false;
        }

        /// <summary>
        /// This hook allows HDRP to give to LookDev what debug mode it can support.
        /// </summary>
        IEnumerable<string> IDataProvider.supportedDebugModes
            => new[]
            {
                "Albedo",
                "Normal",
                "Smoothness",
                "AmbientOcclusion",
                "Metal",
                "Specular",
                "Alpha"
            };

        /// <summary>
        /// This hook allows HDRP to update the debug mode used while requested in the LookDev.
        /// </summary>
        /// <param name="debugIndex">The index corresponding to the debug view, -1 = none, other have same index than iven by IDataProvider.supportedDebugModes</param>
        void IDataProvider.UpdateDebugMode(int debugIndex)
            => debugDisplaySettings.SetDebugViewCommonMaterialProperty((Attributes.MaterialSharedProperty)(debugIndex + 1));

        /// <summary>
        /// This hook allows HDRP to provide a shadow mask in order for LookDev to perform a self shadow composition.
        /// </summary>
        /// <param name="output">The created shadow mask</param>
        /// <param name="SRI">The StageRuntimeInterface allowing to communicate with the LookDev</param>
        void IDataProvider.GetShadowMask(ref RenderTexture output, StageRuntimeInterface SRI)
        {
            LookDevDataForHDRP data = (LookDevDataForHDRP)SRI.SRPData;
            Color oldBackgroundColor = data.additionalCameraData.backgroundColorHDR;
            var oldClearMode = data.additionalCameraData.clearColorMode;
            data.additionalCameraData.backgroundColorHDR = Color.white;
            data.additionalCameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.additionalLightData.intensity = 1f;
            debugDisplaySettings.SetShadowDebugMode(ShadowMapDebugMode.SingleShadow);
            SRI.camera.targetTexture = output;
            SRI.camera.Render();
            debugDisplaySettings.SetShadowDebugMode(ShadowMapDebugMode.None);
            data.additionalLightData.intensity = 0f;
            data.additionalCameraData.backgroundColorHDR = oldBackgroundColor;
            data.additionalCameraData.clearColorMode = oldClearMode;
        }

        void IDataProvider.Cleanup(StageRuntimeInterface SRI)
        {
            LookDevDataForHDRP data = (LookDevDataForHDRP)SRI.SRPData;
            CoreUtils.Destroy(data.volume.sharedProfile);
        }
    }
}
