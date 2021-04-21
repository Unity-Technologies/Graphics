using System;
using System.Collections.Generic; //needed for list of Custom Post Processes injections
using System.IO;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    enum ShaderVariantLogLevel
    {
        Disabled,
        OnlyHDRPShaders,
        AllShaders,
    }

    enum LensAttenuationMode
    {
        ImperfectLens,
        PerfectLens
    }

    /// <summary>
    /// High Definition Render Pipeline's Global Settings.
    /// Global settings are unique per Render Pipeline type. In HD, Global Settings contain:
    /// - a default Volume (Global) combined with its Default Profile (defines which components are active by default)
    /// - the default Volume's profile
    /// - the LookDev Volume Profile
    /// - Frame Settings applied by default to Camera, ReflectionProbe
    /// - Various resources (such as Shaders) for runtime, editor-only, and raytracing
    /// </summary>
    partial class HDRenderPipelineGlobalSettings : RenderPipelineGlobalSettings
    {
        private static HDRenderPipelineGlobalSettings cachedInstance = null;
        public static HDRenderPipelineGlobalSettings instance
        {
            get
            {
                if (cachedInstance == null)
                    cachedInstance = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>() as HDRenderPipelineGlobalSettings;
                return cachedInstance;
            }
        }

        static internal void UpdateGraphicsSettings(HDRenderPipelineGlobalSettings newSettings)
        {
            if (newSettings == null || newSettings == cachedInstance)
                return;
            GraphicsSettings.RegisterRenderPipelineSettings<HDRenderPipeline>(newSettings as RenderPipelineGlobalSettings);
            cachedInstance = newSettings;
        }

#if UNITY_EDITOR
        //Making sure there is at least one HDRenderPipelineGlobalSettings instance in the project
        static internal HDRenderPipelineGlobalSettings Ensure(string folderPath = "HDRPDefaultResources", bool canCreateNewAsset = true)
        {
            bool needsMigration = (assetToBeMigrated != null && !assetToBeMigrated.Equals(null));

            if (HDRenderPipelineGlobalSettings.instance && !needsMigration)
                return HDRenderPipelineGlobalSettings.instance;

            HDRenderPipelineGlobalSettings assetCreated = null;
            string path = "Assets/" + folderPath + "/HDRenderPipelineGlobalSettings.asset";
            if (needsMigration)
            {
                if (HDRenderPipelineGlobalSettings.instance)
                    path = AssetDatabase.GetAssetPath(HDRenderPipelineGlobalSettings.instance);
                else if (!AssetDatabase.IsValidFolder("Assets/" + folderPath))
                    AssetDatabase.CreateFolder("Assets", folderPath);

                assetCreated = MigrateFromHDRPAsset(assetToBeMigrated, path, bClearObsoleteFields: false, canCreateNewAsset: canCreateNewAsset);
                if (assetCreated != null && !assetCreated.Equals(null))
                    assetToBeMigrated = null;
            }
            else
            {
                assetCreated = AssetDatabase.LoadAssetAtPath<HDRenderPipelineGlobalSettings>(path);
                if (assetCreated == null)
                {
                    var guidHDGlobalAssets = AssetDatabase.FindAssets("t:HDRenderPipelineGlobalSettings");
                    //If we could not find the asset at the default path, find the first one
                    if (guidHDGlobalAssets.Length > 0)
                    {
                        var curGUID = guidHDGlobalAssets[0];
                        path = AssetDatabase.GUIDToAssetPath(curGUID);
                        assetCreated = AssetDatabase.LoadAssetAtPath<HDRenderPipelineGlobalSettings>(path);
                    }
                    else if (canCreateNewAsset)// or create one altogether
                    {
                        if (!AssetDatabase.IsValidFolder("Assets/" + folderPath))
                            AssetDatabase.CreateFolder("Assets", folderPath);
                        assetCreated = Create(path);

                        Debug.LogWarning("No HDRP Global Settings Asset is assigned. One has been created for you. If you want to modify it, go to Project Settings > Graphics > HDRP Settings.");
                    }
                    else
                    {
                        Debug.LogError("Cannot migrate HDRP Asset to a new HDRP Global Settings asset. If you are building a Player, make sure to save an HDRP Global Settings asset by opening the project in the Editor.");
                        return null;
                    }
                }
            }
            Debug.Assert(assetCreated, "Could not create HDRP's Global Settings - HDRP may not work correctly - Open the Graphics Window for additional help.");
            UpdateGraphicsSettings(assetCreated);
            return HDRenderPipelineGlobalSettings.instance;
        }

#endif

        void Init()
        {
            if (beforeTransparentCustomPostProcesses == null)
            {
                beforeTransparentCustomPostProcesses = new List<string>();
                beforePostProcessCustomPostProcesses = new List<string>();
                afterPostProcessCustomPostProcesses = new List<string>();
                beforeTAACustomPostProcesses = new List<string>();
            }

            lightLayerName0 = "Light Layer default";
            lightLayerName1 = "Light Layer 1";
            lightLayerName2 = "Light Layer 2";
            lightLayerName3 = "Light Layer 3";
            lightLayerName4 = "Light Layer 4";
            lightLayerName5 = "Light Layer 5";
            lightLayerName6 = "Light Layer 6";
            lightLayerName7 = "Light Layer 7";

            decalLayerName0 = "Decal Layer default";
            decalLayerName1 = "Decal Layer 1";
            decalLayerName2 = "Decal Layer 2";
            decalLayerName3 = "Decal Layer 3";
            decalLayerName4 = "Decal Layer 4";
            decalLayerName5 = "Decal Layer 5";
            decalLayerName6 = "Decal Layer 6";
            decalLayerName7 = "Decal Layer 7";

            UpdateRenderingLayerNames();

            shaderVariantLogLevel = ShaderVariantLogLevel.Disabled;
        }

#if UNITY_EDITOR

        static HDRenderPipelineAsset assetToBeMigrated = null;
        internal static void MigrateFromHDRPAsset(HDRenderPipelineAsset oldAsset)
        {
            assetToBeMigrated = oldAsset;
        }

        internal static HDRenderPipelineGlobalSettings MigrateFromHDRPAsset(HDRenderPipelineAsset oldAsset, string path, bool bClearObsoleteFields = true, bool canCreateNewAsset = true)
        {
            HDRenderPipelineGlobalSettings assetCreated = null;

            // 1. Load or Create the HDAsset and save it on disk
            assetCreated = AssetDatabase.LoadAssetAtPath<HDRenderPipelineGlobalSettings>(path);
            if (assetCreated == null)
            {
                if (!canCreateNewAsset)
                {
                    Debug.LogError("Cannot migrate HDRP Asset to a new HDRP Global Settings asset. If you are building a Player, make sure to save an HDRP Global Settings asset by opening the project in the Editor.");
                    return null;
                }
                assetCreated = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();
                AssetDatabase.CreateAsset(assetCreated, path);
                assetCreated.Init();
            }

#pragma warning disable 618 // Type or member is obsolete
            //2. Migrate obsolete assets (version DefaultSettingsAsAnAsset)
            assetCreated.volumeProfile        = oldAsset.m_ObsoleteDefaultVolumeProfile;
            assetCreated.lookDevVolumeProfile = oldAsset.m_ObsoleteDefaultLookDevProfile;

            assetCreated.m_RenderingPathDefaultCameraFrameSettings                  = oldAsset.m_ObsoleteFrameSettingsMovedToDefaultSettings;
            assetCreated.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = oldAsset.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings;
            assetCreated.m_RenderingPathDefaultRealtimeReflectionFrameSettings      = oldAsset.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings;

            assetCreated.m_RenderPipelineResources           = oldAsset.m_ObsoleteRenderPipelineResources;
            assetCreated.m_RenderPipelineRayTracingResources = oldAsset.m_ObsoleteRenderPipelineRayTracingResources;

            assetCreated.beforeTransparentCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTransparentCustomPostProcesses);
            assetCreated.beforePostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforePostProcessCustomPostProcesses);
            assetCreated.afterPostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteAfterPostProcessCustomPostProcesses);
            assetCreated.beforeTAACustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTAACustomPostProcesses);

            assetCreated.lightLayerName0 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName0);
            assetCreated.lightLayerName1 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName1);
            assetCreated.lightLayerName2 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName2);
            assetCreated.lightLayerName3 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName3);
            assetCreated.lightLayerName4 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName4);
            assetCreated.lightLayerName5 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName5);
            assetCreated.lightLayerName6 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName6);
            assetCreated.lightLayerName7 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName7);

            // Decal layer names were added in 2021 cycle
            if (oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName0 != null)
            {
                assetCreated.decalLayerName0 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName0);
                assetCreated.decalLayerName1 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName1);
                assetCreated.decalLayerName2 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName2);
                assetCreated.decalLayerName3 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName3);
                assetCreated.decalLayerName4 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName4);
                assetCreated.decalLayerName5 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName5);
                assetCreated.decalLayerName6 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName6);
                assetCreated.decalLayerName7 = System.String.Copy(oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName7);
            }

            assetCreated.shaderVariantLogLevel = oldAsset.m_ObsoleteShaderVariantLogLevel;
            assetCreated.lensAttenuationMode = oldAsset.m_ObsoleteLensAttenuation;

            // we need to make sure the old diffusion profile had time to upgrade before moving it away
            if (oldAsset.diffusionProfileSettings != null)
            {
                oldAsset.diffusionProfileSettings.TryToUpgrade();
            }

            int oldSize = oldAsset.m_ObsoleteDiffusionProfileSettingsList?.Length ?? 0;
            System.Array.Resize(ref assetCreated.diffusionProfileSettingsList, oldSize);
            for (int i = 0; i < oldSize; ++i)
                assetCreated.diffusionProfileSettingsList[i] = oldAsset.m_ObsoleteDiffusionProfileSettingsList[i];

            //3. Clear obsolete fields
            if (bClearObsoleteFields)
            {
                oldAsset.m_ObsoleteDefaultVolumeProfile = null;
                oldAsset.m_ObsoleteDefaultLookDevProfile = null;

                oldAsset.m_ObsoleteRenderPipelineResources = null;
                oldAsset.m_ObsoleteRenderPipelineRayTracingResources = null;

                oldAsset.m_ObsoleteBeforeTransparentCustomPostProcesses = null;
                oldAsset.m_ObsoleteBeforePostProcessCustomPostProcesses = null;
                oldAsset.m_ObsoleteAfterPostProcessCustomPostProcesses = null;
                oldAsset.m_ObsoleteBeforeTAACustomPostProcesses = null;
                oldAsset.m_ObsoleteDiffusionProfileSettingsList = null;
            }
#pragma warning restore 618

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return assetCreated;
        }

        internal static HDRenderPipelineGlobalSettings Create(string path, HDRenderPipelineGlobalSettings src = null)
        {
            HDRenderPipelineGlobalSettings assetCreated = null;

            // make sure the asset does not already exists
            assetCreated = AssetDatabase.LoadAssetAtPath<HDRenderPipelineGlobalSettings>(path);
            if (assetCreated == null)
            {
                assetCreated = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();
                AssetDatabase.CreateAsset(assetCreated, path);
                assetCreated.Init();
                if (assetCreated != null)
                {
                    assetCreated.name = Path.GetFileName(path);
                }
            }

            if (assetCreated)
            {
#if UNITY_EDITOR
                assetCreated.EnsureEditorResources(forceReload: true);
#endif
                if (src != null)
                {
                    assetCreated.renderPipelineResources = src.renderPipelineResources;
                    assetCreated.renderPipelineRayTracingResources = src.renderPipelineRayTracingResources;

                    assetCreated.volumeProfile = src.volumeProfile;
                    assetCreated.lookDevVolumeProfile = src.lookDevVolumeProfile;

                    assetCreated.m_RenderingPathDefaultCameraFrameSettings = src.m_RenderingPathDefaultCameraFrameSettings;
                    assetCreated.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = src.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings;
                    assetCreated.m_RenderingPathDefaultRealtimeReflectionFrameSettings = src.m_RenderingPathDefaultRealtimeReflectionFrameSettings;

                    assetCreated.beforeTransparentCustomPostProcesses.AddRange(src.beforeTransparentCustomPostProcesses);
                    assetCreated.beforePostProcessCustomPostProcesses.AddRange(src.beforePostProcessCustomPostProcesses);
                    assetCreated.afterPostProcessCustomPostProcesses.AddRange(src.afterPostProcessCustomPostProcesses);
                    assetCreated.beforeTAACustomPostProcesses.AddRange(src.beforeTAACustomPostProcesses);

                    assetCreated.lightLayerName0 = System.String.Copy(src.lightLayerName0);
                    assetCreated.lightLayerName1 = System.String.Copy(src.lightLayerName1);
                    assetCreated.lightLayerName2 = System.String.Copy(src.lightLayerName2);
                    assetCreated.lightLayerName3 = System.String.Copy(src.lightLayerName3);
                    assetCreated.lightLayerName4 = System.String.Copy(src.lightLayerName4);
                    assetCreated.lightLayerName5 = System.String.Copy(src.lightLayerName5);
                    assetCreated.lightLayerName6 = System.String.Copy(src.lightLayerName6);
                    assetCreated.lightLayerName7 = System.String.Copy(src.lightLayerName7);

                    assetCreated.decalLayerName0 = System.String.Copy(src.decalLayerName0);
                    assetCreated.decalLayerName1 = System.String.Copy(src.decalLayerName1);
                    assetCreated.decalLayerName2 = System.String.Copy(src.decalLayerName2);
                    assetCreated.decalLayerName3 = System.String.Copy(src.decalLayerName3);
                    assetCreated.decalLayerName4 = System.String.Copy(src.decalLayerName4);
                    assetCreated.decalLayerName5 = System.String.Copy(src.decalLayerName5);
                    assetCreated.decalLayerName6 = System.String.Copy(src.decalLayerName6);
                    assetCreated.decalLayerName7 = System.String.Copy(src.decalLayerName7);

                    assetCreated.shaderVariantLogLevel = src.shaderVariantLogLevel;
                    assetCreated.lensAttenuationMode = src.lensAttenuationMode;

                    System.Array.Resize(ref assetCreated.diffusionProfileSettingsList, src.diffusionProfileSettingsList.Length);
                    for (int i = 0; i < src.diffusionProfileSettingsList.Length; ++i)
                        assetCreated.diffusionProfileSettingsList[i] = src.diffusionProfileSettingsList[i];
                }
                else
                {
                    assetCreated.EnsureRuntimeResources(forceReload: false);
                    assetCreated.EnsureRayTracingResources(forceReload: false);
                    assetCreated.GetOrCreateDefaultVolumeProfile();
                    assetCreated.GetOrAssignLookDevVolumeProfile();
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return assetCreated;
        }

#endif // UNITY_EDITOR

        #region Volume
        private Volume s_DefaultVolume = null;

        internal Volume GetOrCreateDefaultVolume()
        {
            if (s_DefaultVolume == null || s_DefaultVolume.Equals(null))
            {
                var go = new GameObject("Default Volume") { hideFlags = HideFlags.HideAndDontSave };
                s_DefaultVolume = go.AddComponent<Volume>();
                s_DefaultVolume.isGlobal = true;
                s_DefaultVolume.priority = float.MinValue;
                s_DefaultVolume.sharedProfile = GetOrCreateDefaultVolumeProfile();
#if UNITY_EDITOR
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
                {
                    DestroyDefaultVolume();
                };
#endif
            }

            if (
                // In case the asset was deleted or the reference removed
                s_DefaultVolume.sharedProfile == null || s_DefaultVolume.sharedProfile.Equals(null)
#if UNITY_EDITOR
                // In case the serialization recreated an empty volume sharedProfile
                || !UnityEditor.AssetDatabase.Contains(s_DefaultVolume.sharedProfile)
#endif
            )
            {
                s_DefaultVolume.sharedProfile = volumeProfile;
            }

            if (s_DefaultVolume.sharedProfile != volumeProfile)
            {
                s_DefaultVolume.sharedProfile = volumeProfile;
            }

            if (s_DefaultVolume == null)
            {
                Debug.LogError("[HDRP] Cannot Create Default Volume.");
            }

            return s_DefaultVolume;
        }

#if UNITY_EDITOR
        private void DestroyDefaultVolume()
        {
            if (s_DefaultVolume != null && !s_DefaultVolume.Equals(null))
            {
                CoreUtils.Destroy(s_DefaultVolume.gameObject);
                s_DefaultVolume = null;
            }
        }

#endif

        #endregion

        #region VolumeProfile

        [SerializeField, FormerlySerializedAs("m_VolumeProfileDefault")]
        private VolumeProfile m_DefaultVolumeProfile;

        internal VolumeProfile volumeProfile
        {
            get => m_DefaultVolumeProfile;
            set => m_DefaultVolumeProfile = value;
        }

        /// <summary>Get the current default VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal VolumeProfile GetOrCreateDefaultVolumeProfile()
        {
#if UNITY_EDITOR
            if (volumeProfile == null || volumeProfile.Equals(null))
            {
                volumeProfile = renderPipelineEditorResources.defaultSettingsVolumeProfile;
            }
#endif
            return volumeProfile;
        }

#if UNITY_EDITOR
        internal bool IsVolumeProfileFromResources()
        {
            return volumeProfile != null && !volumeProfile.Equals(null) && renderPipelineEditorResources != null && volumeProfile.Equals(renderPipelineEditorResources.defaultSettingsVolumeProfile);
        }

#endif

        #endregion

        #region Look Dev Profile
#if UNITY_EDITOR
        [SerializeField, FormerlySerializedAs("VolumeProfileLookDev")]
        private VolumeProfile m_LookDevVolumeProfile;

        internal VolumeProfile lookDevVolumeProfile
        {
            get => m_LookDevVolumeProfile;
            set => m_LookDevVolumeProfile = value;
        }

        internal VolumeProfile GetOrAssignLookDevVolumeProfile()
        {
            if (lookDevVolumeProfile == null || lookDevVolumeProfile.Equals(null))
            {
                lookDevVolumeProfile = renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile;
            }
            return lookDevVolumeProfile;
        }

        internal bool IsVolumeProfileLookDevFromResources()
        {
            return lookDevVolumeProfile != null && !lookDevVolumeProfile.Equals(null) && renderPipelineEditorResources != null && lookDevVolumeProfile.Equals(renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile);
        }

#endif
        #endregion

        #region Camera's FrameSettings
        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_ActiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        [SerializeField]
        FrameSettings m_RenderingPathDefaultCameraFrameSettings = FrameSettings.NewDefaultCamera();

        [SerializeField]
        FrameSettings m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = FrameSettings.NewDefaultCustomOrBakeReflectionProbe();

        [SerializeField]
        FrameSettings m_RenderingPathDefaultRealtimeReflectionFrameSettings = FrameSettings.NewDefaultRealtimeReflectionProbe();

        internal ref FrameSettings GetDefaultFrameSettings(FrameSettingsRenderType type)
        {
            switch (type)
            {
                case FrameSettingsRenderType.Camera:
                    return ref m_RenderingPathDefaultCameraFrameSettings;
                case FrameSettingsRenderType.CustomOrBakedReflection:
                    return ref m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings;
                case FrameSettingsRenderType.RealtimeReflection:
                    return ref m_RenderingPathDefaultRealtimeReflectionFrameSettings;
                default:
                    throw new System.ArgumentException("Unknown FrameSettingsRenderType");
            }
        }

        #endregion

        #region Resource Common
#if UNITY_EDITOR
        void EnsureResources<T>(bool forceReload, ref T resources, string resourcePath, Func<HDRenderPipelineGlobalSettings, bool> checker)
            where T : HDRenderPipelineResources
        {
            T resourceChecked = null;

            if (checker(this))
            {
                if (!EditorUtility.IsPersistent(resources)) // if not loaded from the Asset database
                {
                    // try to load from AssetDatabase if it is ready
                    resourceChecked = AssetDatabase.LoadAssetAtPath<T>(resourcePath);
                    if (resourceChecked && !resourceChecked.Equals(null))
                        resources = resourceChecked;
                }
                if (forceReload)
                    ResourceReloader.ReloadAllNullIn(resources, HDUtils.GetHDRenderPipelinePath());
                return;
            }

            resourceChecked = AssetDatabase.LoadAssetAtPath<T>(resourcePath);
            if (resourceChecked != null && !resourceChecked.Equals(null))
            {
                resources = resourceChecked;
                if (forceReload)
                    ResourceReloader.ReloadAllNullIn(resources, HDUtils.GetHDRenderPipelinePath());
            }
            else // Asset database may not be ready
            {
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
                resources = (objs != null && objs.Length > 0) ? objs[0] as T : null;
                if (forceReload)
                {
                    if (ResourceReloader.ReloadAllNullIn(resources, HDUtils.GetHDRenderPipelinePath()))
                    {
                        InternalEditorUtility.SaveToSerializedFileAndForget(
                            new Object[] { resources },
                            resourcePath,
                            true);
                    }
                }
            }
            Debug.Assert(checker(this), $"Could not load {typeof(T).Name}.");
        }

#endif
        #endregion //Resource Common

        #region Runtime Resources
        [SerializeField]
        HDRenderPipelineRuntimeResources m_RenderPipelineResources;

        internal HDRenderPipelineRuntimeResources renderPipelineResources
        {
            get
            {
#if UNITY_EDITOR
                EnsureRuntimeResources(forceReload: false);
#endif
                return m_RenderPipelineResources;
            }
            set { m_RenderPipelineResources = value; }
        }

#if UNITY_EDITOR
        // be sure to cach result for not using GC in a frame after first one.
        static readonly string runtimeResourcesPath = HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRuntimeResources.asset";

        internal void EnsureRuntimeResources(bool forceReload)
            => EnsureResources(forceReload, ref m_RenderPipelineResources, runtimeResourcesPath, AreRuntimeResourcesCreated_Internal);

        // Passing method in a Func argument create a functor that create GC
        // If it is static it is then only computed once but the Ensure is called after first frame which will make our GC check fail
        // So create it once and store it here.
        // Expected usage: HDRenderPipelineGlobalSettings.AreRuntimeResourcesCreated(anyHDRenderPipelineGlobalSettings) that will return a bool
        static Func<HDRenderPipelineGlobalSettings, bool> AreRuntimeResourcesCreated_Internal = global
            => global.m_RenderPipelineResources != null && !global.m_RenderPipelineResources.Equals(null);

        internal bool AreRuntimeResourcesCreated() => AreRuntimeResourcesCreated_Internal(this);

        internal void EnsureShadersCompiled()
        {
            // We iterate over all compute shader to verify if they are all compiled, if it's not the case
            // then we throw an exception to avoid allocating resources and crashing later on by using a null
            // compute kernel.
            foreach (var computeShader in m_RenderPipelineResources.shaders.GetAllComputeShaders())
            {
                foreach (var message in UnityEditor.ShaderUtil.GetComputeShaderMessages(computeShader))
                {
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    {
                        // Will be catched by the try in HDRenderPipelineAsset.CreatePipeline()
                        throw new System.Exception(System.String.Format(
                            "Compute Shader compilation error on platform {0} in file {1}:{2}: {3}{4}\n" +
                            "HDRP will not run until the error is fixed.\n",
                            message.platform, message.file, message.line, message.message, message.messageDetails
                        ));
                    }
                }
            }
        }

#endif //UNITY_EDITOR
        #endregion // Runtime Resources

        #region Editor Resources (not serialized)
#if UNITY_EDITOR
        HDRenderPipelineEditorResources m_RenderPipelineEditorResources;
        internal HDRenderPipelineEditorResources renderPipelineEditorResources
        {
            get
            {
                //there is no clean way to load editor resources without having it serialized
                // - impossible to load them at deserialization
                // - constructor only called at asset creation
                // - cannot rely on OnEnable
                //thus fallback with lazy init for them
                EnsureEditorResources(forceReload: false);
                return m_RenderPipelineEditorResources;
            }
        }

        // be sure to cach result for not using GC in a frame after first one.
        static readonly string editorResourcesPath = HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset";

        internal void EnsureEditorResources(bool forceReload)
            => EnsureResources(forceReload, ref m_RenderPipelineEditorResources, editorResourcesPath, AreEditorResourcesCreated_Internal);

        // Passing method in a Func argument create a functor that create GC
        // If it is static it is then only computed once but the Ensure is called after first frame which will make our GC check fail
        // So create it once and store it here.
        // Expected usage: HDRenderPipelineGlobalSettings.AreEditorResourcesCreated(anyHDRenderPipelineGlobalSettings) that will return a bool
        static Func<HDRenderPipelineGlobalSettings, bool> AreEditorResourcesCreated_Internal = global
            => global.m_RenderPipelineEditorResources != null && !global.m_RenderPipelineEditorResources.Equals(null);

        internal bool AreEditorResourcesCreated() => AreEditorResourcesCreated_Internal(this);

        // Note: This function is HD specific
        /// <summary>HDRP default Decal material.</summary>
        public Material GetDefaultDecalMaterial()
            => m_RenderPipelineEditorResources.materials.defaultDecalMat;

        // Note: This function is HD specific
        /// <summary>HDRP default mirror material.</summary>
        public Material GetDefaultMirrorMaterial()
            => m_RenderPipelineEditorResources.materials.defaultMirrorMat;
#endif

        #endregion //Editor Resources

        #region Ray Tracing Resources
        [SerializeField]
        HDRenderPipelineRayTracingResources m_RenderPipelineRayTracingResources;
        internal HDRenderPipelineRayTracingResources renderPipelineRayTracingResources
        {
            get { return m_RenderPipelineRayTracingResources; }
            set { m_RenderPipelineRayTracingResources = value; }
        }

#if UNITY_EDITOR
        // be sure to cach result for not using GC in a frame after first one.
        static readonly string raytracingResourcesPath = HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRayTracingResources.asset";

        internal void EnsureRayTracingResources(bool forceReload)
            => EnsureResources(forceReload, ref m_RenderPipelineRayTracingResources, raytracingResourcesPath, AreRayTracingResourcesCreated_Internal);

        internal void ClearRayTracingResources()
            => m_RenderPipelineRayTracingResources = null;

        // Passing method in a Func argument create a functor that create GC
        // If it is static it is then only computed once but the Ensure is called after first frame which will make our GC check fail
        // So create it once and store it here.
        // Expected usage: HDRenderPipelineGlobalSettings.AreRayTracingResourcesCreated(anyHDRenderPipelineGlobalSettings) that will return a bool
        static Func<HDRenderPipelineGlobalSettings, bool> AreRayTracingResourcesCreated_Internal = global
            => global.m_RenderPipelineRayTracingResources != null && !global.m_RenderPipelineRayTracingResources.Equals(null);

        internal bool AreRayTracingResourcesCreated() => AreRayTracingResourcesCreated_Internal(this);
#endif

        #endregion //Ray Tracing Resources

        #region Custom Post Processes Injections

        // List of custom post process Types that will be executed in the project, in the order of the list (top to back)
        [SerializeField]
        internal List<string> beforeTransparentCustomPostProcesses = new List<string>();
        [SerializeField]
        internal List<string> beforePostProcessCustomPostProcesses = new List<string>();
        [SerializeField]
        internal List<string> afterPostProcessCustomPostProcesses = new List<string>();
        [SerializeField]
        internal List<string> beforeTAACustomPostProcesses = new List<string>();

        #endregion

        #region Rendering Layer Names [Light + Decal]

        /// <summary>Name for light layer 0.</summary>
        public string lightLayerName0;
        /// <summary>Name for light layer 1.</summary>
        public string lightLayerName1;
        /// <summary>Name for light layer 2.</summary>
        public string lightLayerName2;
        /// <summary>Name for light layer 3.</summary>
        public string lightLayerName3;
        /// <summary>Name for light layer 4.</summary>
        public string lightLayerName4;
        /// <summary>Name for light layer 5.</summary>
        public string lightLayerName5;
        /// <summary>Name for light layer 6.</summary>
        public string lightLayerName6;
        /// <summary>Name for light layer 7.</summary>
        public string lightLayerName7;


        [System.NonSerialized]
        string[] m_LightLayerNames = null;
        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        public string[] lightLayerNames
        {
            get
            {
                if (m_LightLayerNames == null)
                {
                    m_LightLayerNames = new string[8];
                }

                m_LightLayerNames[0] = lightLayerName0;
                m_LightLayerNames[1] = lightLayerName1;
                m_LightLayerNames[2] = lightLayerName2;
                m_LightLayerNames[3] = lightLayerName3;
                m_LightLayerNames[4] = lightLayerName4;
                m_LightLayerNames[5] = lightLayerName5;
                m_LightLayerNames[6] = lightLayerName6;
                m_LightLayerNames[7] = lightLayerName7;

                return m_LightLayerNames;
            }
        }

        /// <summary>Name for decal layer 0.</summary>
        public string decalLayerName0;
        /// <summary>Name for decal layer 1.</summary>
        public string decalLayerName1;
        /// <summary>Name for decal layer 2.</summary>
        public string decalLayerName2;
        /// <summary>Name for decal layer 3.</summary>
        public string decalLayerName3;
        /// <summary>Name for decal layer 4.</summary>
        public string decalLayerName4;
        /// <summary>Name for decal layer 5.</summary>
        public string decalLayerName5;
        /// <summary>Name for decal layer 6.</summary>
        public string decalLayerName6;
        /// <summary>Name for decal layer 7.</summary>
        public string decalLayerName7;

        [System.NonSerialized]
        string[] m_DecalLayerNames = null;
        /// <summary>
        /// Names used for display of decal layers.
        /// </summary>
        public string[] decalLayerNames
        {
            get
            {
                if (m_DecalLayerNames == null)
                {
                    m_DecalLayerNames = new string[8];
                }

                m_DecalLayerNames[0] = decalLayerName0;
                m_DecalLayerNames[1] = decalLayerName1;
                m_DecalLayerNames[2] = decalLayerName2;
                m_DecalLayerNames[3] = decalLayerName3;
                m_DecalLayerNames[4] = decalLayerName4;
                m_DecalLayerNames[5] = decalLayerName5;
                m_DecalLayerNames[6] = decalLayerName6;
                m_DecalLayerNames[7] = decalLayerName7;

                return m_DecalLayerNames;
            }
        }


        // HDRP use GetRenderingLayerMaskNames to create its light linking system
        // Mean here we define our name for light linking.
        [System.NonSerialized]
        string[] m_RenderingLayerNames;
        string[] renderingLayerNames
        {
            get
            {
                if (m_RenderingLayerNames == null)
                {
                    UpdateRenderingLayerNames();
                }

                return m_RenderingLayerNames;
            }
        }
        public string[] renderingLayerMaskNames => renderingLayerNames;

        void UpdateRenderingLayerNames()
        {
            m_RenderingLayerNames = new string[32];

            m_RenderingLayerNames[0] = lightLayerName0;
            m_RenderingLayerNames[1] = lightLayerName1;
            m_RenderingLayerNames[2] = lightLayerName2;
            m_RenderingLayerNames[3] = lightLayerName3;
            m_RenderingLayerNames[4] = lightLayerName4;
            m_RenderingLayerNames[5] = lightLayerName5;
            m_RenderingLayerNames[6] = lightLayerName6;
            m_RenderingLayerNames[7] = lightLayerName7;

            m_RenderingLayerNames[8]  = decalLayerName0;
            m_RenderingLayerNames[9]  = decalLayerName1;
            m_RenderingLayerNames[10] = decalLayerName2;
            m_RenderingLayerNames[11] = decalLayerName3;
            m_RenderingLayerNames[12] = decalLayerName4;
            m_RenderingLayerNames[13] = decalLayerName5;
            m_RenderingLayerNames[14] = decalLayerName6;
            m_RenderingLayerNames[15] = decalLayerName7;

            // Unused
            for (int i = 16; i < m_RenderingLayerNames.Length; ++i)
            {
                m_RenderingLayerNames[i] = string.Format("Unused {0}", i);
            }
        }

        #endregion

        #region Misc.

        [SerializeField]
        internal ShaderVariantLogLevel shaderVariantLogLevel = ShaderVariantLogLevel.Disabled;

        [SerializeField]
        internal LensAttenuationMode lensAttenuationMode;

        [SerializeField]
        internal DiffusionProfileSettings[] diffusionProfileSettingsList = new DiffusionProfileSettings[0];

#if UNITY_EDITOR
        internal bool AddDiffusionProfile(DiffusionProfileSettings profile)
        {
            if (diffusionProfileSettingsList.Length < 15)
            {
                int index = diffusionProfileSettingsList.Length;
                System.Array.Resize(ref diffusionProfileSettingsList, index + 1);
                diffusionProfileSettingsList[index] = profile;
                UnityEditor.EditorUtility.SetDirty(this);
                return true;
            }
            else
            {
                Debug.LogErrorFormat("We cannot add the diffusion profile {0} to the HDRP's Global Settings as we only allow 14 custom profiles. Please remove one before adding a new one.", profile.name);
                return false;
            }
        }

#endif

        #endregion
    }
}
