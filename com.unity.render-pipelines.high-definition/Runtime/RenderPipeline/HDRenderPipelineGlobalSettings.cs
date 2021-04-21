using System.Collections.Generic; //needed for list of Custom Post Processes injections
using System.IO;
using System.Linq;
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
        static internal HDRenderPipelineGlobalSettings Ensure(bool canCreateNewAsset = true)
        {
            if (instance != null && !instance.Equals(null))
            {
                //if there is an instance but at first version, check for migrating HDRPAsset data from GraphicSettings
                if (instance.m_Version == Version.First
                    && GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset hdrpAsset
                    && hdrpAsset.IsVersionBelowAddedHDRenderPipelineGlobalSettings())
                    (hdrpAsset as IMigratableAsset).Migrate(); //this will call MigrateFromHDRPAsset with this
            }
            else
            {
                //if there is no instance, check for migrating HDRPAsset data from GraphicSettings into a fresh one
                if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset hdrpAsset
                    && hdrpAsset.IsVersionBelowAddedHDRenderPipelineGlobalSettings())
                    (hdrpAsset as IMigratableAsset).Migrate(); //this will call MigrateFromHDRPAsset with this
                else
                {
                    //try load at default path
                    HDRenderPipelineGlobalSettings loaded = AssetDatabase.LoadAssetAtPath<HDRenderPipelineGlobalSettings>($"Assets/{HDProjectSettings.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset");

                    if (loaded == null)
                    {
                        //Use any available
                        IEnumerator<HDRenderPipelineGlobalSettings> enumerator = CoreUtils.LoadAllAssets<HDRenderPipelineGlobalSettings>().GetEnumerator();
                        if (enumerator.MoveNext())
                            loaded = enumerator.Current;
                    }

                    if (loaded != null)
                        UpdateGraphicsSettings(loaded);
                }

                // No migration available and no asset available? Create one if allowed
                if (canCreateNewAsset && instance == null)
                {
                    var createdAsset = Create($"Assets/{HDProjectSettings.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset");
                    UpdateGraphicsSettings(createdAsset);

                    Debug.LogWarning("No HDRP Global Settings Asset is assigned. One has been created for you. If you want to modify it, go to Project Settings > Graphics > HDRP Settings.");
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

        internal static void MigrateFromHDRPAsset(HDRenderPipelineAsset oldAsset)
        {
            if (instance != null
                && !instance.Equals(null)
                && instance.m_Version != Version.First)
                return;

            //1. Create the instance or load current one if at first version
            HDRenderPipelineGlobalSettings assetToUpgrade;

            if (instance == null || instance.Equals(null))
            {
                assetToUpgrade = Create($"Assets/{HDProjectSettings.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset");
                UpdateGraphicsSettings(assetToUpgrade);
            }
            else
                assetToUpgrade = instance;

            Debug.Assert(assetToUpgrade);

            //2. Migrate obsolete assets (version DefaultSettingsAsAnAsset)
#pragma warning disable 618 // Type or member is obsolete
            assetToUpgrade.volumeProfile        = oldAsset.m_ObsoleteDefaultVolumeProfile;
            assetToUpgrade.lookDevVolumeProfile = oldAsset.m_ObsoleteDefaultLookDevProfile;

            assetToUpgrade.m_RenderingPathDefaultCameraFrameSettings                  = oldAsset.m_ObsoleteFrameSettingsMovedToDefaultSettings;
            assetToUpgrade.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = oldAsset.m_ObsoleteBakedOrCustomReflectionFrameSettingsMovedToDefaultSettings;
            assetToUpgrade.m_RenderingPathDefaultRealtimeReflectionFrameSettings      = oldAsset.m_ObsoleteRealtimeReflectionFrameSettingsMovedToDefaultSettings;

            assetToUpgrade.m_RenderPipelineResources           = oldAsset.m_ObsoleteRenderPipelineResources;
            assetToUpgrade.m_RenderPipelineRayTracingResources = oldAsset.m_ObsoleteRenderPipelineRayTracingResources;

            assetToUpgrade.beforeTransparentCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTransparentCustomPostProcesses);
            assetToUpgrade.beforePostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforePostProcessCustomPostProcesses);
            assetToUpgrade.afterPostProcessCustomPostProcesses.AddRange(oldAsset.m_ObsoleteAfterPostProcessCustomPostProcesses);
            assetToUpgrade.beforeTAACustomPostProcesses.AddRange(oldAsset.m_ObsoleteBeforeTAACustomPostProcesses);

            assetToUpgrade.lightLayerName0 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName0;
            assetToUpgrade.lightLayerName1 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName1;
            assetToUpgrade.lightLayerName2 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName2;
            assetToUpgrade.lightLayerName3 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName3;
            assetToUpgrade.lightLayerName4 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName4;
            assetToUpgrade.lightLayerName5 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName5;
            assetToUpgrade.lightLayerName6 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName6;
            assetToUpgrade.lightLayerName7 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteLightLayerName7;

            // Decal layer names were added in 2021 cycle
            if (oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName0 != null)
            {
                assetToUpgrade.decalLayerName0 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName0;
                assetToUpgrade.decalLayerName1 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName1;
                assetToUpgrade.decalLayerName2 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName2;
                assetToUpgrade.decalLayerName3 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName3;
                assetToUpgrade.decalLayerName4 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName4;
                assetToUpgrade.decalLayerName5 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName5;
                assetToUpgrade.decalLayerName6 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName6;
                assetToUpgrade.decalLayerName7 = oldAsset.currentPlatformRenderPipelineSettings.m_ObsoleteDecalLayerName7;
            }

            assetToUpgrade.shaderVariantLogLevel = oldAsset.m_ObsoleteShaderVariantLogLevel;
            assetToUpgrade.lensAttenuationMode = oldAsset.m_ObsoleteLensAttenuation;

            // we need to make sure the old diffusion profile had time to upgrade before moving it away
            if (oldAsset.diffusionProfileSettings != null)
            {
                oldAsset.diffusionProfileSettings.TryToUpgrade();
            }

            int oldSize = oldAsset.m_ObsoleteDiffusionProfileSettingsList?.Length ?? 0;
            System.Array.Resize(ref assetToUpgrade.diffusionProfileSettingsList, oldSize);
            for (int i = 0; i < oldSize; ++i)
                assetToUpgrade.diffusionProfileSettingsList[i] = oldAsset.m_ObsoleteDiffusionProfileSettingsList[i];
#pragma warning restore 618

            //3. Set version to next & Launch remaining of migration
            assetToUpgrade.m_Version = Version.MigratedFromHDRPAssetOrCreated;
            (assetToUpgrade as IMigratableAsset).Migrate();
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
            AssetDatabase.CreateAsset(assetCreated, path);
            Debug.Assert(assetCreated);
            assetCreated.name = Path.GetFileName(path);

            // copy data from provided source
            if (dataSource != null)
            {
                assetCreated.renderPipelineEditorResources = dataSource.renderPipelineEditorResources;
                assetCreated.renderPipelineResources = dataSource.renderPipelineResources;
                assetCreated.renderPipelineRayTracingResources = dataSource.renderPipelineRayTracingResources;

                assetCreated.volumeProfile = dataSource.volumeProfile;
                assetCreated.lookDevVolumeProfile = dataSource.lookDevVolumeProfile;

                assetCreated.m_RenderingPathDefaultCameraFrameSettings = dataSource.m_RenderingPathDefaultCameraFrameSettings;
                assetCreated.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings = dataSource.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings;
                assetCreated.m_RenderingPathDefaultRealtimeReflectionFrameSettings = dataSource.m_RenderingPathDefaultRealtimeReflectionFrameSettings;

                assetCreated.beforeTransparentCustomPostProcesses.AddRange(dataSource.beforeTransparentCustomPostProcesses);
                assetCreated.beforePostProcessCustomPostProcesses.AddRange(dataSource.beforePostProcessCustomPostProcesses);
                assetCreated.afterPostProcessCustomPostProcesses.AddRange(dataSource.afterPostProcessCustomPostProcesses);
                assetCreated.beforeTAACustomPostProcesses.AddRange(dataSource.beforeTAACustomPostProcesses);

                assetCreated.lightLayerName0 = dataSource.lightLayerName0;
                assetCreated.lightLayerName1 = dataSource.lightLayerName1;
                assetCreated.lightLayerName2 = dataSource.lightLayerName2;
                assetCreated.lightLayerName3 = dataSource.lightLayerName3;
                assetCreated.lightLayerName4 = dataSource.lightLayerName4;
                assetCreated.lightLayerName5 = dataSource.lightLayerName5;
                assetCreated.lightLayerName6 = dataSource.lightLayerName6;
                assetCreated.lightLayerName7 = dataSource.lightLayerName7;

                assetCreated.decalLayerName0 = dataSource.decalLayerName0;
                assetCreated.decalLayerName1 = dataSource.decalLayerName1;
                assetCreated.decalLayerName2 = dataSource.decalLayerName2;
                assetCreated.decalLayerName3 = dataSource.decalLayerName3;
                assetCreated.decalLayerName4 = dataSource.decalLayerName4;
                assetCreated.decalLayerName5 = dataSource.decalLayerName5;
                assetCreated.decalLayerName6 = dataSource.decalLayerName6;
                assetCreated.decalLayerName7 = dataSource.decalLayerName7;

                assetCreated.shaderVariantLogLevel = dataSource.shaderVariantLogLevel;
                assetCreated.lensAttenuationMode = dataSource.lensAttenuationMode;

                System.Array.Resize(ref assetCreated.diffusionProfileSettingsList, dataSource.diffusionProfileSettingsList.Length);
                for (int i = 0; i < dataSource.diffusionProfileSettingsList.Length; ++i)
                    assetCreated.diffusionProfileSettingsList[i] = dataSource.diffusionProfileSettingsList[i];
            }

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

        #region Runtime Resources

        [SerializeField]
        RenderPipelineResources m_RenderPipelineResources;

        internal RenderPipelineResources renderPipelineResources
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
        string runtimeResourcesPath => HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset";

        internal void EnsureRuntimeResources(bool forceReload)
        {
            RenderPipelineResources resources = null;
            if (AreResourcesCreated())
            {
                if (!EditorUtility.IsPersistent(m_RenderPipelineResources))
                {
                    resources = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(runtimeResourcesPath);
                    if (resources && !resources.Equals(null))
                        m_RenderPipelineResources = resources;
                }
                return;
            }

            resources = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(runtimeResourcesPath);
            if (resources && !resources.Equals(null))
            {
                m_RenderPipelineResources = resources;
            }
            else
            {
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(runtimeResourcesPath);
                m_RenderPipelineResources = objs != null && objs.Length > 0 ? objs.First() as RenderPipelineResources : null;

                if (forceReload)
                {
                    if (ResourceReloader.ReloadAllNullIn(m_RenderPipelineResources, HDUtils.GetHDRenderPipelinePath()))
                    {
                        InternalEditorUtility.SaveToSerializedFileAndForget(
                            new UnityEngine.Object[] { m_RenderPipelineResources },
                            runtimeResourcesPath,
                            true);
                    }
                }
            }
            Debug.Assert(AreResourcesCreated(), "Could not load Runtime Resources for HDRP.");
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
            private set => m_RenderPipelineEditorResources = value;
        }

        string editorResourcesPath => HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset";

        internal void EnsureEditorResources(bool forceReload)
        {
            HDRenderPipelineEditorResources resources = null;

            if (AreEditorResourcesCreated())
            {
                if (!EditorUtility.IsPersistent(m_RenderPipelineEditorResources)) // if not loaded from the Asset database
                {
                    // try to load from AssetDatabase if it is ready
                    resources = AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(editorResourcesPath);
                    if (resources && !resources.Equals(null))
                    {
                        m_RenderPipelineEditorResources = resources;
                    }
                }
                return;
            }
            resources = AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(editorResourcesPath);
            if (resources && !resources.Equals(null))
            {
                m_RenderPipelineEditorResources = resources;
            }
            else // Asset database may not be ready
            {
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
            }
            Debug.Assert(AreEditorResourcesCreated(), "Could not load Editor Resources.");
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
        string raytracingResourcesPath => HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRayTracingResources.asset";

        internal void EnsureRayTracingResources(bool forceReload)
        {
            HDRenderPipelineRayTracingResources resources = null;

            if (AreRayTracingResourcesCreated())
            {
                if (!EditorUtility.IsPersistent(m_RenderPipelineRayTracingResources))
                {
                    resources = AssetDatabase.LoadAssetAtPath<HDRenderPipelineRayTracingResources>(raytracingResourcesPath);
                    if (resources && !resources.Equals(null))
                    {
                        m_RenderPipelineRayTracingResources = resources;
                    }
                }
                return;
            }
            resources = AssetDatabase.LoadAssetAtPath<HDRenderPipelineRayTracingResources>(raytracingResourcesPath);
            if (resources && !resources.Equals(null))
            {
                m_RenderPipelineRayTracingResources = resources;
            }
            else
            {
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(raytracingResourcesPath);
                m_RenderPipelineRayTracingResources = (objs != null && objs.Length > 0) ? objs[0] as HDRenderPipelineRayTracingResources : null;
                if (forceReload)
                {
#if UNITY_EDITOR_LINUX // Temp hack to be able to make linux test run. To clarify
                    ResourceReloader.TryReloadAllNullIn(m_RenderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
#else
                    if (ResourceReloader.ReloadAllNullIn(m_RenderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath()))
                    {
                        InternalEditorUtility.SaveToSerializedFileAndForget(
                            new Object[] { m_RenderPipelineRayTracingResources },
                            raytracingResourcesPath,
                            true);
                    }
#endif
                    Debug.Assert(AreRayTracingResourcesCreated(), $"Could not load Ray Tracing Resources from {raytracingResourcesPath}.");
                }
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

        #region Rendering Layer Names [Light + Decal]

        /// <summary>Name for light layer 0.</summary>
        public string lightLayerName0 = "Light Layer default";
        /// <summary>Name for light layer 1.</summary>
        public string lightLayerName1 = "Light Layer 1";
        /// <summary>Name for light layer 2.</summary>
        public string lightLayerName2 = "Light Layer 2";
        /// <summary>Name for light layer 3.</summary>
        public string lightLayerName3 = "Light Layer 3";
        /// <summary>Name for light layer 4.</summary>
        public string lightLayerName4 = "Light Layer 4";
        /// <summary>Name for light layer 5.</summary>
        public string lightLayerName5 = "Light Layer 5";
        /// <summary>Name for light layer 6.</summary>
        public string lightLayerName6 = "Light Layer 6";
        /// <summary>Name for light layer 7.</summary>
        public string lightLayerName7 = "Light Layer 7";


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
        public string decalLayerName0 = "Decal Layer default";
        /// <summary>Name for decal layer 1.</summary>
        public string decalLayerName1 = "Decal Layer 1";
        /// <summary>Name for decal layer 2.</summary>
        public string decalLayerName2 = "Decal Layer 2";
        /// <summary>Name for decal layer 3.</summary>
        public string decalLayerName3 = "Decal Layer 3";
        /// <summary>Name for decal layer 4.</summary>
        public string decalLayerName4 = "Decal Layer 4";
        /// <summary>Name for decal layer 5.</summary>
        public string decalLayerName5 = "Decal Layer 5";
        /// <summary>Name for decal layer 6.</summary>
        public string decalLayerName6 = "Decal Layer 6";
        /// <summary>Name for decal layer 7.</summary>
        public string decalLayerName7 = "Decal Layer 7";

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
