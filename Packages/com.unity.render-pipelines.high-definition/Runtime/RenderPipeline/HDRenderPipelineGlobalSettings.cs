using System;
using System.Collections.Generic;
using System.ComponentModel; //needed for list of Custom Post Processes injections
using System.IO;
using System.Linq;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    enum LensAttenuationMode
    {
        ImperfectLens,
        PerfectLens
    }

    enum ColorGradingSpace
    {
        AcesCg = 0,
        sRGB        // Legacy.
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
    [HDRPHelpURL("Default-Settings-Window")]
    [DisplayInfo(name = "HDRP Global Settings Asset", order = CoreUtils.Sections.section4 + 2)]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [DisplayName("HDRP")]
    partial class HDRenderPipelineGlobalSettings : RenderPipelineGlobalSettings<HDRenderPipelineGlobalSettings, HDRenderPipeline>
    {
        [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
        protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;

#if UNITY_EDITOR
        internal static string defaultPath => $"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset";

        //Making sure there is at least one HDRenderPipelineGlobalSettings instance in the project
        internal static HDRenderPipelineGlobalSettings Ensure(bool canCreateNewAsset = true)
        {
            HDRenderPipelineGlobalSettings currentInstance = GraphicsSettings.
                GetSettingsForRenderPipeline<HDRenderPipeline>() as HDRenderPipelineGlobalSettings;

            if (currentInstance == null || currentInstance.Equals(null) || currentInstance.m_Version == Version.First)
            {
                // Try to migrate HDRPAsset in Graphics. It can produce a HDRenderPipelineGlobalSettings
                // with data from former HDRPAsset if it is at a version allowing this.
                if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset hdrpAsset &&
                    hdrpAsset is IMigratableAsset migratableAsset &&
                    hdrpAsset.IsVersionBelowAddedHDRenderPipelineGlobalSettings())
                {
                    // if possible we need to finish migration of hdrpAsset in order to grab value from it
                    migratableAsset.Migrate();

                    // the migration of the HDRP asset has updated the current instance
                    currentInstance =
                        GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>() as
                            HDRenderPipelineGlobalSettings;
                }
            }

            if (RenderPipelineGlobalSettingsUtils.TryEnsure<HDRenderPipelineGlobalSettings, HDRenderPipeline>(ref currentInstance, defaultPath, canCreateNewAsset))
            {
                if (currentInstance is IMigratableAsset migratableAsset && !migratableAsset.IsAtLastVersion())
                {
                    migratableAsset.Migrate();
                    EditorUtility.SetDirty(currentInstance);
                    AssetDatabase.SaveAssetIfDirty(currentInstance);
                }

                return currentInstance;
            }

            return null;
        }

        void Reset()
        {
            m_PrefixedRenderingLayerNames = null;
        }

        public override void Initialize(RenderPipelineGlobalSettings source = null)
        {
            // ensure resources are here
            EnsureEditorResources(forceReload: true);
            EnsureRuntimeResources(forceReload: true);
            EnsureRayTracingResources(forceReload: true);
            GetOrCreateDefaultVolumeProfile();
            GetOrAssignLookDevVolumeProfile();
        }

#endif // UNITY_EDITOR

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
                volumeProfile = CopyVolumeProfileFromResourcesToAssets(renderPipelineEditorResources.defaultSettingsVolumeProfile);
            }
#endif
            return volumeProfile;
        }

#if UNITY_EDITOR
        internal bool IsVolumeProfileFromResources()
        {
            return volumeProfile != null && !volumeProfile.Equals(null) && renderPipelineEditorResources != null && volumeProfile.Equals(renderPipelineEditorResources.defaultSettingsVolumeProfile);
        }

        static VolumeProfile CopyVolumeProfileFromResourcesToAssets(VolumeProfile profileInResourcesFolder)
        {
            string path = $"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}/{profileInResourcesFolder.name}.asset";
            if (!UnityEditor.AssetDatabase.IsValidFolder($"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", HDProjectSettingsReadOnlyBase.projectSettingsFolderPath);

            //try load one if one already exist
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null || profile.Equals(null))
            {
                //else create it
                UnityEditor.AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(profileInResourcesFolder), path);
                profile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            }

            return profile;
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
                lookDevVolumeProfile = CopyVolumeProfileFromResourcesToAssets(renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile);
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
        [SerializeField, FormerlySerializedAs("m_RenderingPathDefaultCameraFrameSettings"), Obsolete("Kept For Migration. #from(2023.2")]
        FrameSettings m_ObsoleteRenderingPathDefaultCameraFrameSettings = FrameSettingsDefaults.Get(FrameSettingsRenderType.Camera);
        
        [SerializeField, FormerlySerializedAs("m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings"), Obsolete("Kept For Migration. #from(2023.2")]
        FrameSettings m_ObsoleteRenderingPathDefaultBakedOrCustomReflectionFrameSettings = FrameSettingsDefaults.Get(FrameSettingsRenderType.CustomOrBakedReflection);

        [SerializeField, FormerlySerializedAs("m_RenderingPathDefaultRealtimeReflectionFrameSettings"), Obsolete("Kept For Migration. #from(2023.2")]
        FrameSettings m_ObsoleteRenderingPathDefaultRealtimeReflectionFrameSettings = FrameSettingsDefaults.Get(FrameSettingsRenderType.RealtimeReflection);

        [SerializeField] private RenderingPathFrameSettings m_RenderingPath = new();

        internal ref FrameSettings GetDefaultFrameSettings(FrameSettingsRenderType type)
        {
            return ref m_RenderingPath.GetDefaultFrameSettings(type);
        }

        #endregion

        #region Resource Common
#if UNITY_EDITOR
        // Yes it is stupid to retry right away but making it called in EditorApplication.delayCall
        // from EnsureResources create GC
        void DelayedNullReload<T>(string resourcePath)
            where T : HDRenderPipelineResources
        {
            T resourcesDelayed = AssetDatabase.LoadAssetAtPath<T>(resourcePath);
            if (resourcesDelayed == null)
                EditorApplication.delayCall += () => DelayedNullReload<T>(resourcePath);
            else
                ResourceReloader.ReloadAllNullIn(resourcesDelayed, HDUtils.GetHDRenderPipelinePath());
        }

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
            else
            {
                // Asset database may not be ready
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
                resources = (objs != null && objs.Length > 0) ? objs[0] as T : null;
                if (forceReload)
                {
                    try
                    {
                        if (ResourceReloader.ReloadAllNullIn(resources, HDUtils.GetHDRenderPipelinePath()))
                        {
                            InternalEditorUtility.SaveToSerializedFileAndForget(
                                new Object[] { resources },
                                resourcePath,
                                true);
                        }
                    }
                    catch (InvalidImportException)
                    {
                        // This can be called at a time where AssetDatabase is not available for loading.
                        // When this happens, the GUID can be get but the resource loaded will be null.
                        // Using the ResourceReloader mechanism in CoreRP, it checks this and add InvalidImport data when this occurs.
                        DelayedNullReload<T>(resourcePath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
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
            get
            {
                // No ensure because it can be null if we do not use ray tracing
                return m_RenderPipelineRayTracingResources;
            }
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
        internal List<string> afterPostProcessBlursCustomPostProcesses = new List<string>();
        [SerializeField]
        internal List<string> afterPostProcessCustomPostProcesses = new List<string>();
        [SerializeField]
        internal List<string> beforeTAACustomPostProcesses = new List<string>();

        /// <summary>
        /// Returns true if the custom post process type in parameter has been registered
        /// </summary>
        /// <param name="customPostProcessType"></param>
        public bool IsCustomPostProcessRegistered(Type customPostProcessType)
        {
            string type = customPostProcessType.AssemblyQualifiedName;
            return beforeTransparentCustomPostProcesses.Contains(type)
                || beforePostProcessCustomPostProcesses.Contains(type)
                || afterPostProcessBlursCustomPostProcesses.Contains(type)
                || afterPostProcessCustomPostProcesses.Contains(type)
                || beforeTAACustomPostProcesses.Contains(type);
        }

        #endregion

        #region Rendering Layer Mask

        [SerializeField]
        internal RenderingLayerMask defaultRenderingLayerMask = RenderingLayerMask.Default;

        [SerializeField]
        internal string[] renderingLayerNames = null;

        [System.NonSerialized]
        string[] m_PrefixedRenderingLayerNames;
        internal string[] prefixedRenderingLayerNames
        {
            get
            {
                if (m_PrefixedRenderingLayerNames == null)
                    UpdateRenderingLayerNames();
                return m_PrefixedRenderingLayerNames;
            }
        }

        /// <summary>Regenerate Rendering Layer names and their prefixed versions.</summary>
        internal void UpdateRenderingLayerNames()
        {
            if (renderingLayerNames == null)
                renderingLayerNames = new string[1];
            if (m_PrefixedRenderingLayerNames == null)
                m_PrefixedRenderingLayerNames = new string[16];

            for (int i = 0; i < m_PrefixedRenderingLayerNames.Length; ++i)
            {
                if (i < renderingLayerNames.Length && renderingLayerNames[i] == null) renderingLayerNames[i] = GetDefaultLayerName(i);
                m_PrefixedRenderingLayerNames[i] = i < renderingLayerNames.Length ? renderingLayerNames[i] : $"Unused Layer {i}";
            }
        }

        internal void ResetRenderingLayerNames()
        {
            for (int i = 0; i < renderingLayerNames.Length; ++i)
                renderingLayerNames[i] = GetDefaultLayerName(i);
            UpdateRenderingLayerNames();
        }

        internal static string GetDefaultLayerName(int index)
        {
            return index == 0 ? "Default" : $"Layer {index}";
        }

        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName0;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName1;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName2;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName3;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName4;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName5;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName6;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName7;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName0;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName1;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName2;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName3;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName4;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName5;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName6;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName7;

        #endregion

        #region Misc.
        [SerializeField]
        internal LensAttenuationMode lensAttenuationMode;

        [SerializeField]
        internal ColorGradingSpace colorGradingSpace;

        [SerializeField, FormerlySerializedAs("diffusionProfileSettingsList")]
        internal DiffusionProfileSettings[] m_ObsoleteDiffusionProfileSettingsList;

        [SerializeField]
        internal bool specularFade;

        [SerializeField]
        internal bool rendererListCulling;

        static readonly DiffusionProfileSettings[] kEmptyProfiles = new DiffusionProfileSettings[0];
        internal DiffusionProfileSettings[] diffusionProfileSettingsList
        {
            get
            {
                if (instance.volumeProfile != null && instance.volumeProfile.TryGet<DiffusionProfileList>(out var overrides))
                    return overrides.diffusionProfiles.value ?? kEmptyProfiles;
                return kEmptyProfiles;
            }
            set { GetOrCreateDiffusionProfileList().diffusionProfiles.value = value; }
        }

        internal DiffusionProfileList GetOrCreateDiffusionProfileList()
        {
            var volumeProfile = instance.GetOrCreateDefaultVolumeProfile();
            if (!volumeProfile.TryGet<DiffusionProfileList>(out var component))
            {
                component = volumeProfile.Add<DiffusionProfileList>(true);

#if UNITY_EDITOR
                if (EditorUtility.IsPersistent(volumeProfile))
                {
                    UnityEditor.AssetDatabase.AddObjectToAsset(component, volumeProfile);
                    EditorUtility.SetDirty(volumeProfile);
                }
#endif
            }

            if (component.diffusionProfiles.value == null)
                component.diffusionProfiles.value = new DiffusionProfileSettings[0];
            return component;
        }

#if UNITY_EDITOR
        internal void TryAutoRegisterDiffusionProfile(DiffusionProfileSettings profile)
        {
            if (!autoRegisterDiffusionProfiles || profile == null || diffusionProfileSettingsList == null || diffusionProfileSettingsList.Any(d => d == profile))
                return;

            if (diffusionProfileSettingsList.Length >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
            {
                Debug.LogError($"Failed to register profile '{profile.name}'. You have reached the limit of {DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1} custom diffusion profiles in HDRP's Global Settings Default Volume.\n" +
                        "Remove one from the list or disable the automatic registration of missing diffusion profiles in the Global Settings.", HDRenderPipelineGlobalSettings.instance);
                return;
            }

            AddDiffusionProfile(profile);
        }

        internal bool AddDiffusionProfile(DiffusionProfileSettings profile)
        {
            var overrides = GetOrCreateDiffusionProfileList();
            var profiles = overrides.diffusionProfiles.value;

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] == null)
                {
                    profiles[i] = profile;
                    return true;
                }
            }

            if (profiles.Length >= DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1)
            {
                Debug.LogErrorFormat("Failed to register profile {0}. You have reached the limit of {1} custom diffusion profiles in HDRP's Global Settings Default Volume. Please remove one before adding a new one.", profile.name, DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT - 1);
                return false;
            }

            int index = profiles.Length;
            Array.Resize(ref profiles, index + 1);
            profiles[index] = profile;

            overrides.diffusionProfiles.value = profiles;
            EditorUtility.SetDirty(overrides);
            return true;
        }

#endif

        [SerializeField]
        [Obsolete("This field is not used anymore. #from(2023.2)")]
        internal string DLSSProjectId = "000000";

        [SerializeField]
        [Obsolete("This field is not used anymore. #from(2023.2)")]
        internal bool useDLSSCustomProjectId = false;

        [SerializeField]
        internal bool supportProbeVolumes = false;

        /// <summary>
        /// Controls whether diffusion profiles referenced by an imported material should be automatically added to the list.
        /// </summary>
        public bool autoRegisterDiffusionProfiles = true;


        public bool analyticDerivativeEmulation = false;

        public bool analyticDerivativeDebugOutput = false;

        #endregion

        #region APV
        // This is temporarily here until we have a core place to put it shared between pipelines.
        [SerializeField]
        internal ProbeVolumeSceneData apvScenesData;

        internal ProbeVolumeSceneData GetOrCreateAPVSceneData()
        {
            if (apvScenesData == null)
                apvScenesData = new ProbeVolumeSceneData(this);

            apvScenesData.SetParentObject(this);
            return apvScenesData;
        }

        #endregion
    }
}
