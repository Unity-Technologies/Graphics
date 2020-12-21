using UnityEngine.Rendering;
using System.Collections.Generic; //needed for list of Custom Post Processes injections
using System.IO;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;
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
    /// High Definition Render Pipeline Default Settings.
    /// Default settings are unique per Render Pipeline type. In HD, Default Settings contain:
    /// - a default Volume (Global) combined with its Default Profile (defines which components are active by default)
    /// - this Volume's profile
    /// - the LookDev Volume Profile
    /// - Frame Settings
    /// - Various resources for runtime, editor-only, and raytracing
    /// </summary>
    public partial class HDDefaultSettings : RenderPipelineDefaultSettings
    {
        private static HDDefaultSettings cachedInstance = null;
        public static HDDefaultSettings instance
        {
            get
            {
                if (cachedInstance == null)
                    cachedInstance = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>() as HDDefaultSettings;
                return cachedInstance;
            }
        }

        static public void UpdateGraphicsSettings(HDDefaultSettings newSettings)
        {
            if (newSettings == null || newSettings == cachedInstance)
                return;
            GraphicsSettings.RegisterRenderPipelineSettings<HDRenderPipeline>(newSettings as RenderPipelineDefaultSettings);
            cachedInstance = newSettings;
        }

#if UNITY_EDITOR
        //Making sure there is at least one HDDefaultSettings instance in the project
        static public HDDefaultSettings Ensure()
        {
            if (HDDefaultSettings.instance)
                return HDDefaultSettings.instance;

            HDDefaultSettings assetCreated = null;
            string path = "Assets/HDRPDefaultResources/HDGraphicsSettings.asset";
            assetCreated = AssetDatabase.LoadAssetAtPath<HDDefaultSettings>(path);
            if (assetCreated == null)
            {
                var guidHDDefaultAssets = AssetDatabase.FindAssets("t:HDDefaultSettings");
                //If we could not find the asset at the default path, find the first one
                if (guidHDDefaultAssets.Length > 0)
                {
                    var curGUID = guidHDDefaultAssets[0];
                    path = AssetDatabase.GUIDToAssetPath(curGUID);
                    assetCreated = AssetDatabase.LoadAssetAtPath<HDDefaultSettings>(path);
                }
                else // or create one altogether
                {
                    if (!AssetDatabase.IsValidFolder("Assets/HDRPDefaultResources/"))
                        AssetDatabase.CreateFolder("Assets", "HDRPDefaultResources");
                    assetCreated = ScriptableObject.CreateInstance<HDDefaultSettings>();
                    AssetDatabase.CreateAsset(assetCreated, path);
                    assetCreated.Init();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            Debug.Assert(assetCreated, "Could not create HD Default Settings - HDRP may not work correctly - Open the Graphics Window for additional help.");
            UpdateGraphicsSettings(assetCreated);
            return HDDefaultSettings.instance;
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

            shaderVariantLogLevel = ShaderVariantLogLevel.Disabled;

#if UNITY_EDITOR
            EnsureEditorResources(forceReload: false);
#endif
        }

        #if UNITY_EDITOR
        internal static HDDefaultSettings MigrateFromHDRPAsset(HDRenderPipelineAsset oldAsset, bool bClearObsoleteFields = true)
        {
            string path = "Assets/HDRPDefaultResources/HDGraphicsSettings.asset";
            return MigrateFromHDRPAsset(oldAsset, path, bClearObsoleteFields);
        }

        internal static HDDefaultSettings MigrateFromHDRPAsset(HDRenderPipelineAsset oldAsset, string path, bool bClearObsoleteFields = true)
        {
            HDDefaultSettings assetCreated = null;

            // 1. Load or Create the HDAsset and save it on disk
            assetCreated = AssetDatabase.LoadAssetAtPath<HDDefaultSettings>(path);
            if (assetCreated == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/HDRPDefaultResources/"))
                    AssetDatabase.CreateFolder("Assets", "HDRPDefaultResources");
                assetCreated = ScriptableObject.CreateInstance<HDDefaultSettings>();
                AssetDatabase.CreateAsset(assetCreated, path);
                assetCreated.Init();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

#pragma warning disable 618 // Type or member is obsolete
            //2. Migrate obsolete assets (version DefaultSettingsAsAnAsset)
            assetCreated.volumeProfile        = oldAsset.m_ObsoleteDefaultVolumeProfile;
            assetCreated.volumeProfileLookDev = oldAsset.m_ObsoleteDefaultLookDevProfile;

            assetCreated.m_RenderingPathDefaultCameraFrameSettings                  = oldAsset.m_ObsoleteFrameSettingsMovedToDefaultSettings;
            assetCreated.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = oldAsset.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings;
            assetCreated.m_RenderingPathDefaultRealtimeReflectionFrameSettings      = oldAsset.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings;

            assetCreated.m_RenderPipelineResources           = oldAsset.m_ObsoleteRenderPipelineResources;
            assetCreated.m_RenderPipelineRayTracingResources = oldAsset.m_ObsoleteRenderPipelineRayTracingResources;

            assetCreated.beforeTransparentCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTransparentCustomPostProcesses);
            assetCreated.beforePostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforePostProcessCustomPostProcesses);
            assetCreated.afterPostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteAfterPostProcessCustomPostProcesses);
            assetCreated.beforeTAACustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTAACustomPostProcesses);

            assetCreated.lightLayerName0 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName0;
            assetCreated.lightLayerName1 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName1;
            assetCreated.lightLayerName2 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName2;
            assetCreated.lightLayerName3 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName3;
            assetCreated.lightLayerName4 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName4;
            assetCreated.lightLayerName5 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName5;
            assetCreated.lightLayerName6 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName6;
            assetCreated.lightLayerName7 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName7;

            assetCreated.decalLayerName0 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName0;
            assetCreated.decalLayerName1 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName1;
            assetCreated.decalLayerName2 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName2;
            assetCreated.decalLayerName3 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName3;
            assetCreated.decalLayerName4 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName4;
            assetCreated.decalLayerName5 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName5;
            assetCreated.decalLayerName6 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName6;
            assetCreated.decalLayerName7 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName7;

            assetCreated.shaderVariantLogLevel = oldAsset.m_ObsoleteShaderVariantLogLevel;
            assetCreated.lensAttenuationMode = oldAsset.m_ObsoleteLensAttenuation;

            System.Array.Resize(ref assetCreated.diffusionProfileSettingsList, oldAsset.m_ObsoleteDiffusionProfileSettingsList.Length);
            for (int i = 0; i < oldAsset.m_ObsoleteDiffusionProfileSettingsList.Length; ++i)
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
                /* TODOJENNY - not sure why we cannot reset a string like that
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName0 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName1 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName2 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName3 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName4 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName5 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName6 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName7 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName0 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName1 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName2 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName3 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName4 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName5 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName6 = "";
                oldAsset.currentPlatformRenderPipelineSettings.m_ObsoletelightLayerName7 = "";
                */
                System.Array.Resize(ref oldAsset.m_ObsoleteDiffusionProfileSettingsList, 0);
            }
#pragma warning restore 618

            return assetCreated;
        }

        internal static HDDefaultSettings Create(string path, HDDefaultSettings src = null)
        {
            HDDefaultSettings assetCreated = null;

            // make sure the asset does not already exists
            assetCreated = AssetDatabase.LoadAssetAtPath<HDDefaultSettings>(path);
            if (assetCreated == null)
            {
                assetCreated = ScriptableObject.CreateInstance<HDDefaultSettings>();
                AssetDatabase.CreateAsset(assetCreated, path);
                assetCreated.Init();
                if (assetCreated != null)
                {
                    assetCreated.name = Path.GetFileName(path);
                }
            }

            if (assetCreated)
            {
                if (src != null)
                {
                    assetCreated.renderPipelineResources = src.renderPipelineResources;
                    assetCreated.renderPipelineRayTracingResources = src.renderPipelineRayTracingResources;

                    assetCreated.volumeProfile = src.volumeProfile;
                    assetCreated.volumeProfileLookDev = src.volumeProfileLookDev;

                    assetCreated.m_RenderingPathDefaultCameraFrameSettings = src.m_RenderingPathDefaultCameraFrameSettings;
                    assetCreated.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = src.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings;
                    assetCreated.m_RenderingPathDefaultRealtimeReflectionFrameSettings = src.m_RenderingPathDefaultRealtimeReflectionFrameSettings;

                    assetCreated.beforeTransparentCustomPostProcesses.AddRange(src.beforeTransparentCustomPostProcesses);
                    assetCreated.beforePostProcessCustomPostProcesses.AddRange(src.beforePostProcessCustomPostProcesses);
                    assetCreated.afterPostProcessCustomPostProcesses.AddRange(src.afterPostProcessCustomPostProcesses);
                    assetCreated.beforeTAACustomPostProcesses.AddRange(src.beforeTAACustomPostProcesses);

                    assetCreated.lightLayerName0 = src.lightLayerName0;
                    assetCreated.lightLayerName1 = src.lightLayerName1;
                    assetCreated.lightLayerName2 = src.lightLayerName2;
                    assetCreated.lightLayerName3 = src.lightLayerName3;
                    assetCreated.lightLayerName4 = src.lightLayerName4;
                    assetCreated.lightLayerName5 = src.lightLayerName5;
                    assetCreated.lightLayerName6 = src.lightLayerName6;
                    assetCreated.lightLayerName7 = src.lightLayerName7;

                    assetCreated.decalLayerName0 = src.decalLayerName0;
                    assetCreated.decalLayerName1 = src.decalLayerName1;
                    assetCreated.decalLayerName2 = src.decalLayerName2;
                    assetCreated.decalLayerName3 = src.decalLayerName3;
                    assetCreated.decalLayerName4 = src.decalLayerName4;
                    assetCreated.decalLayerName5 = src.decalLayerName5;
                    assetCreated.decalLayerName6 = src.decalLayerName6;
                    assetCreated.decalLayerName7 = src.decalLayerName7;

                    assetCreated.shaderVariantLogLevel = src.shaderVariantLogLevel;
                    assetCreated.lensAttenuationMode = src.lensAttenuationMode;

                    System.Array.Resize(ref assetCreated.diffusionProfileSettingsList, src.diffusionProfileSettingsList.Length);
                    for (int i = 0; i < src.diffusionProfileSettingsList.Length; ++i)
                        assetCreated.diffusionProfileSettingsList[i] = src.diffusionProfileSettingsList[i];
                }
                else
                {
                    assetCreated.EnsureResources(forceReload: false);
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

        [SerializeField] private VolumeProfile m_VolumeProfileDefault;

        internal VolumeProfile volumeProfile
        {
            get => m_VolumeProfileDefault;
            set => m_VolumeProfileDefault = value;
        }

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
            return volumeProfile != null && renderPipelineEditorResources != null && volumeProfile.Equals(renderPipelineEditorResources.defaultSettingsVolumeProfile);
        }

    #endif

        #endregion

        #region Look Dev Profile
#if UNITY_EDITOR
        [SerializeField] private VolumeProfile m_VolumeProfileLookDev;

        internal VolumeProfile volumeProfileLookDev
        {
            get => m_VolumeProfileLookDev;
            set => m_VolumeProfileLookDev = value;
        }
#endif

#if UNITY_EDITOR
        internal VolumeProfile GetOrAssignLookDevVolumeProfile()
        {
            if (volumeProfileLookDev == null || volumeProfileLookDev.Equals(null))
            {
                volumeProfileLookDev = renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile;
            }
            return volumeProfileLookDev;
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

        #region Runtime Resources

        [SerializeField]
        RenderPipelineResources m_RenderPipelineResources;

        internal RenderPipelineResources renderPipelineResources
        {
            get
            {
#if UNITY_EDITOR
                EnsureResources(forceReload: false);
#endif
                return m_RenderPipelineResources;
            }
            set { m_RenderPipelineResources = value; }
        }

#if UNITY_EDITOR
        internal void EnsureResources(bool forceReload)
        {
            if (AreResourcesCreated())
                return;

            m_RenderPipelineResources = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset");

            if (forceReload)
            {
#if UNITY_EDITOR_LINUX // Temp hack to be able to make linux test run. To clarify
                ResourceReloader.TryReloadAllNullIn(m_RenderPipelineResources, HDUtils.GetHDRenderPipelinePath());
#else
                ResourceReloader.ReloadAllNullIn(m_RenderPipelineResources, HDUtils.GetHDRenderPipelinePath());
#endif
            }
        }

#endif

        internal bool AreResourcesCreated()
        {
            return (m_RenderPipelineResources != null && !m_RenderPipelineResources.Equals(null));
        }

#if UNITY_EDITOR
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

        internal void EnsureEditorResources(bool forceReload)
        {
            if (AreEditorResourcesCreated())
                return;

            var editorResourcesPath = HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset";
            var objs = InternalEditorUtility.LoadSerializedFileAndForget(editorResourcesPath);
            m_RenderPipelineEditorResources = (objs != null && objs.Length > 0) ? objs[0] as HDRenderPipelineEditorResources : null;
            if (forceReload)
            {
                if (ResourceReloader.ReloadAllNullIn(m_RenderPipelineEditorResources, HDUtils.GetHDRenderPipelinePath()))
                {
                    InternalEditorUtility.SaveToSerializedFileAndForget(
                        new Object[] { m_RenderPipelineEditorResources },
                        editorResourcesPath,
                        true);
                }
            }
            else if (!EditorUtility.IsPersistent(m_RenderPipelineEditorResources))
            {
                m_RenderPipelineEditorResources = AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(editorResourcesPath);
            }
        }

        internal bool AreEditorResourcesCreated()
        {
            return (m_RenderPipelineEditorResources != null && !m_RenderPipelineEditorResources.Equals(null));
        }

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
        internal void EnsureRayTracingResources(bool forceReload)
        {
            if (AreRayTracingResourcesCreated())
                return;

            m_RenderPipelineRayTracingResources = UnityEditor.AssetDatabase.LoadAssetAtPath<HDRenderPipelineRayTracingResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRayTracingResources.asset");

            if (forceReload)
            {
#if UNITY_EDITOR_LINUX // Temp hack to be able to make linux test run. To clarify
                ResourceReloader.TryReloadAllNullIn(m_RenderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
#else
                ResourceReloader.ReloadAllNullIn(m_RenderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
#endif
            }
        }

        internal void ClearRayTracingResources()
        {
            m_RenderPipelineRayTracingResources = null;
        }

        internal bool AreRayTracingResourcesCreated()
        {
            return (m_RenderPipelineRayTracingResources != null && !m_RenderPipelineRayTracingResources.Equals(null));
        }

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

        #region Layer Names [LIGHT]

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

        #endregion

        #region Layer Names [DECAL]

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

        #endregion

        #region Misc.

        [SerializeField]
        internal ShaderVariantLogLevel shaderVariantLogLevel = ShaderVariantLogLevel.Disabled;

        [SerializeField]
        internal LensAttenuationMode lensAttenuationMode;

        [SerializeField]
        internal DiffusionProfileSettings[] diffusionProfileSettingsList = new DiffusionProfileSettings[0];

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
                Debug.LogErrorFormat("We cannot add the diffusion profile {0} to the HDRP default settings as we only allow 14 custom profiles. Please remove one before adding a new one.", profile.name);
                return false;
            }
        }

        #endregion
    }
}
