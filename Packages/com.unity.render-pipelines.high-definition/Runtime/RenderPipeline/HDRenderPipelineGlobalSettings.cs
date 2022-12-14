using System;
using System.Collections.Generic; //needed for list of Custom Post Processes injections
using System.IO;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;
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
    [HDRPHelpURL("Default-Settings-Window")]
    partial class HDRenderPipelineGlobalSettings : RenderPipelineGlobalSettings
    {
        private static HDRenderPipelineGlobalSettings cachedInstance = null;

        /// <summary>
        /// Active HDRP Global Settings asset. If the value is null then no HDRenderPipelineGlobalSettings has been registered to the Graphics Settings with the HDRenderPipeline.
        /// </summary>
        public static HDRenderPipelineGlobalSettings instance
        {
            get
            {
#if !UNITY_EDITOR
                // The HDRP Global Settings could have been changed by script, undo/redo (case 1342987), or file update - file versioning, let us make sure we display the correct one
                // In a Player, we do not need to worry about those changes as we only support loading one
                if (cachedInstance == null)
#endif
                    cachedInstance = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>() as HDRenderPipelineGlobalSettings;
                return cachedInstance;
            }
        }

        static internal void UpdateGraphicsSettings(HDRenderPipelineGlobalSettings newSettings)
        {
            if (newSettings == cachedInstance)
                return;
            if (newSettings != null)
                GraphicsSettings.RegisterRenderPipelineSettings<HDRenderPipeline>(newSettings as RenderPipelineGlobalSettings);
            else
                GraphicsSettings.UnregisterRenderPipelineSettings<HDRenderPipeline>();
            cachedInstance = newSettings;
        }

#if UNITY_EDITOR

        //Making sure there is at least one HDRenderPipelineGlobalSettings instance in the project
        static internal HDRenderPipelineGlobalSettings Ensure(bool canCreateNewAsset = true)
        {
            if (instance == null || instance.Equals(null) || instance.m_Version == Version.First)
            {
                // Try to migrate HDRPAsset in Graphics. It can produce a HDRenderPipelineGlobalSettings
                // with data from former HDRPAsset if it is at a version allowing this.
                if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset hdrpAsset && hdrpAsset.IsVersionBelowAddedHDRenderPipelineGlobalSettings())
                {
                    // if possible we need to finish migration of hdrpAsset in order to grab value from it
                    (hdrpAsset as IMigratableAsset).Migrate();
                }
            }

            if (instance == null || instance.Equals(null))
            {
                //try load at default path
                HDRenderPipelineGlobalSettings loaded = AssetDatabase.LoadAssetAtPath<HDRenderPipelineGlobalSettings>($"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset");

                if (loaded == null)
                {
                    //Use any available
                    IEnumerator<HDRenderPipelineGlobalSettings> enumerator = CoreUtils.LoadAllAssets<HDRenderPipelineGlobalSettings>().GetEnumerator();
                    if (enumerator.MoveNext())
                        loaded = enumerator.Current;
                }

                if (loaded != null)
                    UpdateGraphicsSettings(loaded);

                // No migration available and no asset available? Create one if allowed
                if (canCreateNewAsset && instance == null)
                {
                    var createdAsset = Create($"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset");
                    UpdateGraphicsSettings(createdAsset);

                    Debug.LogWarning("No HDRP Global Settings Asset is assigned. One has been created for you. If you want to modify it, go to Project Settings > Graphics > HDRP Global Settings.");
                }

                if (instance == null)
                    Debug.LogError("Cannot find any HDRP Global Settings asset and Cannot create one from former used HDRP Asset.");

                Debug.Assert(instance, "Could not create HDRP's Global Settings - HDRP may not work correctly - Open the Graphics Window for additional help.");
            }

            // Attempt upgrade (do notiong if up to date)
            IMigratableAsset migratableAsset = instance;
            if (!migratableAsset.IsAtLastVersion())
                migratableAsset.Migrate();

            return instance;
        }

        void Reset()
        {
            UpdateRenderingLayerNames();
        }

        internal static HDRenderPipelineGlobalSettings Create(string path, HDRenderPipelineGlobalSettings dataSource = null)
        {
            HDRenderPipelineGlobalSettings assetCreated = null;

            //ensure folder tree exist
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);

            //prevent any path conflict
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            //asset creation
            assetCreated = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();
            assetCreated.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(assetCreated, path);
            Debug.Assert(assetCreated);

            // copy data from provided source
            if (dataSource != null)
                EditorUtility.CopySerialized(dataSource, assetCreated);

            // ensure resources are here
            assetCreated.EnsureEditorResources(forceReload: true);
            assetCreated.EnsureRuntimeResources(forceReload: true);
            assetCreated.EnsureRayTracingResources(forceReload: true);
            assetCreated.GetOrCreateDefaultVolumeProfile();
            assetCreated.GetOrAssignLookDevVolumeProfile();

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
                    catch (System.Exception e)
                    {
                        // This can be called at a time where AssetDatabase is not available for loading.
                        // When this happens, the GUID can be get but the resource loaded will be null.
                        // Using the ResourceReloader mechanism in CoreRP, it checks this and add InvalidImport data when this occurs.
                        if (!(e.Data.Contains("InvalidImport") && e.Data["InvalidImport"] is int dii && dii == 1))
                            Debug.LogException(e);
                        else
                            DelayedNullReload<T>(resourcePath);
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

        #endregion

        #region Rendering Layer Names [Light + Decal]

        static readonly string[] k_DefaultLightLayerNames = { "Light Layer default", "Light Layer 1", "Light Layer 2", "Light Layer 3", "Light Layer 4", "Light Layer 5", "Light Layer 6", "Light Layer 7" };

        /// <summary>Name for light layer 0.</summary>
        public string lightLayerName0 = k_DefaultLightLayerNames[0];
        /// <summary>Name for light layer 1.</summary>
        public string lightLayerName1 = k_DefaultLightLayerNames[1];
        /// <summary>Name for light layer 2.</summary>
        public string lightLayerName2 = k_DefaultLightLayerNames[2];
        /// <summary>Name for light layer 3.</summary>
        public string lightLayerName3 = k_DefaultLightLayerNames[3];
        /// <summary>Name for light layer 4.</summary>
        public string lightLayerName4 = k_DefaultLightLayerNames[4];
        /// <summary>Name for light layer 5.</summary>
        public string lightLayerName5 = k_DefaultLightLayerNames[5];
        /// <summary>Name for light layer 6.</summary>
        public string lightLayerName6 = k_DefaultLightLayerNames[6];
        /// <summary>Name for light layer 7.</summary>
        public string lightLayerName7 = k_DefaultLightLayerNames[7];


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

        [System.NonSerialized]
        string[] m_PrefixedLightLayerNames = null;
        /// <summary>
        /// Names used for display of light layers with Layer's index as prefix.
        /// For example: "0: Light Layer Default"
        /// </summary>
        public string[] prefixedLightLayerNames
        {
            get
            {
                if (m_PrefixedLightLayerNames == null)
                    UpdateRenderingLayerNames();
                return m_PrefixedLightLayerNames;
            }
        }

        static readonly string[] k_DefaultDecalLayerNames = { "Decal Layer default", "Decal Layer 1", "Decal Layer 2", "Decal Layer 3", "Decal Layer 4", "Decal Layer 5", "Decal Layer 6", "Decal Layer 7" };

        /// <summary>Name for decal layer 0.</summary>
        public string decalLayerName0 = k_DefaultDecalLayerNames[0];
        /// <summary>Name for decal layer 1.</summary>
        public string decalLayerName1 = k_DefaultDecalLayerNames[1];
        /// <summary>Name for decal layer 2.</summary>
        public string decalLayerName2 = k_DefaultDecalLayerNames[2];
        /// <summary>Name for decal layer 3.</summary>
        public string decalLayerName3 = k_DefaultDecalLayerNames[3];
        /// <summary>Name for decal layer 4.</summary>
        public string decalLayerName4 = k_DefaultDecalLayerNames[4];
        /// <summary>Name for decal layer 5.</summary>
        public string decalLayerName5 = k_DefaultDecalLayerNames[5];
        /// <summary>Name for decal layer 6.</summary>
        public string decalLayerName6 = k_DefaultDecalLayerNames[6];
        /// <summary>Name for decal layer 7.</summary>
        public string decalLayerName7 = k_DefaultDecalLayerNames[7];

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

        [System.NonSerialized]
        string[] m_PrefixedDecalLayerNames = null;
        /// <summary>
        /// Names used for display of decal layers with Decal's index as prefix.
        /// For example: "0: Decal Layer Default"
        /// </summary>
        public string[] prefixedDecalLayerNames
        {
            get
            {
                if (m_PrefixedDecalLayerNames == null)
                    UpdateRenderingLayerNames();
                return m_PrefixedDecalLayerNames;
            }
        }

        [System.NonSerialized]
        string[] m_RenderingLayerNames;
        string[] renderingLayerNames
        {
            get
            {
                if (m_RenderingLayerNames == null)
                    UpdateRenderingLayerNames();
                return m_RenderingLayerNames;
            }
        }

        [System.NonSerialized]
        string[] m_PrefixedRenderingLayerNames;
        string[] prefixedRenderingLayerNames
        {
            get
            {
                if (m_PrefixedRenderingLayerNames == null)
                {
                    UpdateRenderingLayerNames();
                }
                return m_PrefixedRenderingLayerNames;
            }
        }

        /// <summary>Names used for display of rendering layer masks.</summary>
        public string[] renderingLayerMaskNames => renderingLayerNames;

        /// <summary>Names used for display of rendering layer masks with a prefix.</summary>
        public string[] prefixedRenderingLayerMaskNames => prefixedRenderingLayerNames;

        /// <summary>Regenerate Rendering Layer names and their prefixed versions.</summary>
        internal void UpdateRenderingLayerNames()
        {
            if (m_RenderingLayerNames == null)
                m_RenderingLayerNames = new string[32];

            m_RenderingLayerNames[0] = lightLayerName0;
            m_RenderingLayerNames[1] = lightLayerName1;
            m_RenderingLayerNames[2] = lightLayerName2;
            m_RenderingLayerNames[3] = lightLayerName3;
            m_RenderingLayerNames[4] = lightLayerName4;
            m_RenderingLayerNames[5] = lightLayerName5;
            m_RenderingLayerNames[6] = lightLayerName6;
            m_RenderingLayerNames[7] = lightLayerName7;

            m_RenderingLayerNames[8] = decalLayerName0;
            m_RenderingLayerNames[9] = decalLayerName1;
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

            // Update prefixed
            if (m_PrefixedRenderingLayerNames == null)
                m_PrefixedRenderingLayerNames = new string[32];
            if (m_PrefixedLightLayerNames == null)
                m_PrefixedLightLayerNames = new string[8];
            if (m_PrefixedDecalLayerNames == null)
                m_PrefixedDecalLayerNames = new string[8];
            for (int i = 0; i < m_PrefixedRenderingLayerNames.Length; ++i)
            {
                m_PrefixedRenderingLayerNames[i] = string.Format("{0}: {1}", i, m_RenderingLayerNames[i]);
                if (i < 8)
                    m_PrefixedLightLayerNames[i] = m_PrefixedRenderingLayerNames[i];
                else if (i < 16)
                    m_PrefixedDecalLayerNames[i - 8] = string.Format("{0}: {1}", i - 8, m_RenderingLayerNames[i]);
            }
        }

        internal void ResetRenderingLayerNames(bool lightLayers, bool decalLayers)
        {
            if (lightLayers)
            {
                lightLayerName0 = k_DefaultLightLayerNames[0];
                lightLayerName1 = k_DefaultLightLayerNames[1];
                lightLayerName2 = k_DefaultLightLayerNames[2];
                lightLayerName3 = k_DefaultLightLayerNames[3];
                lightLayerName4 = k_DefaultLightLayerNames[4];
                lightLayerName5 = k_DefaultLightLayerNames[5];
                lightLayerName6 = k_DefaultLightLayerNames[6];
                lightLayerName7 = k_DefaultLightLayerNames[7];
            }
            if (decalLayers)
            {
                decalLayerName0 = k_DefaultDecalLayerNames[0];
                decalLayerName1 = k_DefaultDecalLayerNames[1];
                decalLayerName2 = k_DefaultDecalLayerNames[2];
                decalLayerName3 = k_DefaultDecalLayerNames[3];
                decalLayerName4 = k_DefaultDecalLayerNames[4];
                decalLayerName5 = k_DefaultDecalLayerNames[5];
                decalLayerName6 = k_DefaultDecalLayerNames[6];
                decalLayerName7 = k_DefaultDecalLayerNames[7];
            }
            UpdateRenderingLayerNames();
        }

        #endregion

        #region Misc.

        [SerializeField]
        internal ShaderVariantLogLevel shaderVariantLogLevel = ShaderVariantLogLevel.Disabled;

        [SerializeField]
        internal LensAttenuationMode lensAttenuationMode;

        [SerializeField]
        internal DiffusionProfileSettings[] diffusionProfileSettingsList = new DiffusionProfileSettings[0];

        [SerializeField]
        internal bool rendererListCulling;

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

        [SerializeField]
        internal string DLSSProjectId = "000000";

        [SerializeField]
        internal bool useDLSSCustomProjectId = false;

        [SerializeField]
        internal bool supportProbeVolumes = false;

        /// <summary>
        /// Controls whether debug display shaders for Rendering Debugger are available in Player builds.
        /// </summary>
        public bool supportRuntimeDebugDisplay = false;

        #endregion

        #region APV
        // This is temporarily here until we have a core place to put it shared between pipelines.
        [SerializeField]
        internal ProbeVolumeSceneData apvScenesData;

        internal ProbeVolumeSceneData GetOrCreateAPVSceneData()
        {
            if (apvScenesData == null)
                apvScenesData = new ProbeVolumeSceneData((Object)this, nameof(apvScenesData));


            apvScenesData.SetParentObject((Object)this, nameof(apvScenesData));
            return apvScenesData;
        }

        #endregion
    }
}
