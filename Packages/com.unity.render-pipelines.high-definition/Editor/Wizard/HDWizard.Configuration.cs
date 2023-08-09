using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditorInternal.VR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Rendering.HighDefinition
{
    enum InclusiveMode
    {
        HDRP = 1 << 0,
        XRManagement = 1 << 1,
        VR = XRManagement | 1 << 2, //XRManagement is inside VR and will be indented
        DXR = 1 << 3,
        DXROptional = DXR | 1 << 4,
    }

    enum QualityScope { Global, CurrentQuality }

    static class InclusiveScopeExtention
    {
        public static bool Contains(this InclusiveMode thisScope, InclusiveMode scope)
            => ((~thisScope) & scope) == 0;
    }

    partial class HDWizard : EditorWindowWithHelpButton
    {
        #region REFLECTION

        //reflect internal legacy enum
        enum LightmapEncodingQualityCopy
        {
            Low = 0,
            Normal = 1,
            High = 2
        }

        static Func<BuildTargetGroup, LightmapEncodingQualityCopy> GetLightmapEncodingQualityForPlatformGroup;
        static Action<BuildTargetGroup, LightmapEncodingQualityCopy> SetLightmapEncodingQualityForPlatformGroup;
        static Func<BuildTarget> CalculateSelectedBuildTarget;
        static Func<BuildTarget, GraphicsDeviceType[]> GetSupportedGraphicsAPIs;
        static Func<BuildTarget, bool> WillEditorUseFirstGraphicsAPI;
        static Action RequestCloseAndRelaunchWithCurrentArguments;
        static Func<BuildTarget, bool> GetStaticBatching;
        static Action<BuildTarget, bool> SetStaticBatching;

        static void LoadReflectionMethods()
        {
            Type playerSettingsType = typeof(PlayerSettings);
            Type playerSettingsEditorType = playerSettingsType.Assembly.GetType("UnityEditor.PlayerSettingsEditor");
            Type lightEncodingQualityType = playerSettingsType.Assembly.GetType("UnityEditor.LightmapEncodingQuality");
            Type editorUserBuildSettingsUtilsType = playerSettingsType.Assembly.GetType("UnityEditor.EditorUserBuildSettingsUtils");
            var qualityVariable = Expression.Variable(lightEncodingQualityType, "quality_internal");
            var buildTargetVariable = Expression.Variable(typeof(BuildTarget), "platform");
            var staticBatchingVariable = Expression.Variable(typeof(int), "staticBatching");
            var dynamicBatchingVariable = Expression.Variable(typeof(int), "DynamicBatching");
            var staticBatchingParameter = Expression.Parameter(typeof(bool), "staticBatching");
            var buildTargetGroupParameter = Expression.Parameter(typeof(BuildTargetGroup), "platformGroup");
            var buildTargetParameter = Expression.Parameter(typeof(BuildTarget), "platform");
            var qualityParameter = Expression.Parameter(typeof(LightmapEncodingQualityCopy), "quality");
            var getLightmapEncodingQualityForPlatformGroupInfo = playerSettingsType.GetMethod("GetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            var setLightmapEncodingQualityForPlatformGroupInfo = playerSettingsType.GetMethod("SetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            var calculateSelectedBuildTargetInfo = editorUserBuildSettingsUtilsType.GetMethod("CalculateSelectedBuildTarget", BindingFlags.Static | BindingFlags.Public);
            var getSupportedGraphicsAPIsInfo = playerSettingsType.GetMethod("GetSupportedGraphicsAPIs", BindingFlags.Static | BindingFlags.NonPublic);
            var getStaticBatchingInfo = playerSettingsType.GetMethod("GetBatchingForPlatform", BindingFlags.Static | BindingFlags.NonPublic);
            var setStaticBatchingInfo = playerSettingsType.GetMethod("SetBatchingForPlatform", BindingFlags.Static | BindingFlags.NonPublic);
            var willEditorUseFirstGraphicsAPIInfo = playerSettingsEditorType.GetMethod("WillEditorUseFirstGraphicsAPI", BindingFlags.Static | BindingFlags.NonPublic);
            var requestCloseAndRelaunchWithCurrentArgumentsInfo = typeof(EditorApplication).GetMethod("RequestCloseAndRelaunchWithCurrentArguments", BindingFlags.Static | BindingFlags.NonPublic);
            var getLightmapEncodingQualityForPlatformGroupBlock = Expression.Block(
                new[] { qualityVariable },
                Expression.Assign(qualityVariable, Expression.Call(getLightmapEncodingQualityForPlatformGroupInfo, buildTargetGroupParameter)),
                Expression.Convert(qualityVariable, typeof(LightmapEncodingQualityCopy))
            );
            var setLightmapEncodingQualityForPlatformGroupBlock = Expression.Block(
                new[] { qualityVariable },
                Expression.Assign(qualityVariable, Expression.Convert(qualityParameter, lightEncodingQualityType)),
                Expression.Call(setLightmapEncodingQualityForPlatformGroupInfo, buildTargetGroupParameter, qualityVariable)
            );
            var getStaticBatchingBlock = Expression.Block(
                new[] { staticBatchingVariable, dynamicBatchingVariable },
                Expression.Call(getStaticBatchingInfo, buildTargetParameter, staticBatchingVariable, dynamicBatchingVariable),
                Expression.Equal(staticBatchingVariable, Expression.Constant(1))
            );
            var setStaticBatchingBlock = Expression.Block(
                new[] { staticBatchingVariable, dynamicBatchingVariable },
                Expression.Call(getStaticBatchingInfo, buildTargetParameter, staticBatchingVariable, dynamicBatchingVariable),
                Expression.Call(setStaticBatchingInfo, buildTargetParameter, Expression.Convert(staticBatchingParameter, typeof(int)), dynamicBatchingVariable)
            );
            var getLightmapEncodingQualityForPlatformGroupLambda = Expression.Lambda<Func<BuildTargetGroup, LightmapEncodingQualityCopy>>(getLightmapEncodingQualityForPlatformGroupBlock, buildTargetGroupParameter);
            var setLightmapEncodingQualityForPlatformGroupLambda = Expression.Lambda<Action<BuildTargetGroup, LightmapEncodingQualityCopy>>(setLightmapEncodingQualityForPlatformGroupBlock, buildTargetGroupParameter, qualityParameter);
            var calculateSelectedBuildTargetLambda = Expression.Lambda<Func<BuildTarget>>(Expression.Call(null, calculateSelectedBuildTargetInfo));
            var getSupportedGraphicsAPIsLambda = Expression.Lambda<Func<BuildTarget, GraphicsDeviceType[]>>(Expression.Call(null, getSupportedGraphicsAPIsInfo, buildTargetParameter), buildTargetParameter);
            var getStaticBatchingLambda = Expression.Lambda<Func<BuildTarget, bool>>(getStaticBatchingBlock, buildTargetParameter);
            var setStaticBatchingLambda = Expression.Lambda<Action<BuildTarget, bool>>(setStaticBatchingBlock, buildTargetParameter, staticBatchingParameter);
            var willEditorUseFirstGraphicsAPILambda = Expression.Lambda<Func<BuildTarget, bool>>(Expression.Call(null, willEditorUseFirstGraphicsAPIInfo, buildTargetParameter), buildTargetParameter);
            var requestCloseAndRelaunchWithCurrentArgumentsLambda = Expression.Lambda<Action>(Expression.Call(null, requestCloseAndRelaunchWithCurrentArgumentsInfo));
            GetLightmapEncodingQualityForPlatformGroup = getLightmapEncodingQualityForPlatformGroupLambda.Compile();
            SetLightmapEncodingQualityForPlatformGroup = setLightmapEncodingQualityForPlatformGroupLambda.Compile();
            CalculateSelectedBuildTarget = calculateSelectedBuildTargetLambda.Compile();
            GetSupportedGraphicsAPIs = getSupportedGraphicsAPIsLambda.Compile();
            GetStaticBatching = getStaticBatchingLambda.Compile();
            SetStaticBatching = setStaticBatchingLambda.Compile();
            WillEditorUseFirstGraphicsAPI = willEditorUseFirstGraphicsAPILambda.Compile();
            RequestCloseAndRelaunchWithCurrentArguments = requestCloseAndRelaunchWithCurrentArgumentsLambda.Compile();
        }

        #endregion

        #region Entry

        struct Entry
        {
            public delegate bool Checker();
            public delegate void Fixer(bool fromAsync);

            public readonly QualityScope scope;
            public readonly InclusiveMode inclusiveScope;
            public readonly Style.ConfigStyle configStyle;
            public readonly Checker check;
            public readonly Fixer fix;
            public readonly int indent;
            public readonly bool forceDisplayCheck;
            public readonly bool skipErrorIcon;
            public readonly bool displayAssetName;

            public Entry(QualityScope scope, InclusiveMode mode, Style.ConfigStyle configStyle, Checker check, Fixer fix, bool forceDisplayCheck = false, bool skipErrorIcon = false, bool displayAssetName = false)
            {
                this.scope = scope;
                this.inclusiveScope = mode;
                this.configStyle = configStyle;
                this.check = check;
                this.fix = fix;
                this.forceDisplayCheck = forceDisplayCheck;
                indent = mode == InclusiveMode.XRManagement ? 1 : 0;
                this.skipErrorIcon = skipErrorIcon;
                this.displayAssetName = displayAssetName;
            }
        }


        [InitializeOnLoadMethod]
        static void InitializeEntryList()
        {
            //Check for playmode has been added to ensure the editor window wont take focus when entering playmode
            if (EditorWindow.HasOpenInstances<HDWizard>() && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update += DelayedRebuildEntryList;

                // Case 1407981: Calling GetWindow in InitializeOnLoadMethod doesn't work and creates a new window instead of getting the existing one.
                void DelayedRebuildEntryList()
                {
                    EditorApplication.update -= DelayedRebuildEntryList;
                    HDWizard window = EditorWindow.GetWindow<HDWizard>(Style.title.text);
                    window.ReBuildEntryList();
                }
            }
        }

        Entry[] BuildEntryList()
        {
            List<Entry> entryList = new List<Entry>();

            // Add the general and XR entries
            entryList.AddRange(new[]
            {
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpColorSpace, IsColorSpaceCorrect, FixColorSpace),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpLightmapEncoding, IsLightmapCorrect, FixLightmap),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpShadow, IsShadowCorrect, FixShadow),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpShadowmask, IsShadowmaskCorrect, FixShadowmask),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpGlobalSettingsAssigned, IsHdrpGlobalSettingsUsedCorrect, FixHdrpGlobalSettingsUsed),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpAssetGraphicsAssigned, IsHdrpAssetGraphicsUsedCorrect, FixHdrpAssetGraphicsUsed),
                new Entry(QualityScope.CurrentQuality, InclusiveMode.HDRP, Style.hdrpAssetQualityAssigned, IsHdrpAssetQualityUsedCorrect, FixHdrpAssetQualityUsed),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpRuntimeResources, IsRuntimeResourcesCorrect, FixRuntimeResources),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpEditorResources, IsEditorResourcesCorrect, FixEditorResources),
                new Entry(QualityScope.CurrentQuality, InclusiveMode.HDRP, Style.hdrpBatcher, IsSRPBatcherCorrect, FixSRPBatcher),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpDiffusionProfile, IsDiffusionProfileCorrect, FixDiffusionProfile),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpLookDevVolumeProfile, IsDefaultLookDevVolumeProfileCorrect, FixDefaultLookDevVolumeProfile),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpVolumeProfile, IsDefaultVolumeProfileCorrect, FixDefaultVolumeProfile),
                new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpMigratableAssets, IsMigratableAssetsCorrect, FixMigratableAssets),

                new Entry(QualityScope.Global, InclusiveMode.VR, Style.vrLegacyVRSystem, IsOldVRSystemForCurrentBuildTargetGroupCorrect, FixOldVRSystemForCurrentBuildTargetGroup),
                new Entry(QualityScope.Global, InclusiveMode.VR, Style.vrXRManagementPackage, IsVRXRManagementPackageInstalledCorrect, FixVRXRManagementPackageInstalled),
                new Entry(QualityScope.Global, InclusiveMode.XRManagement, Style.vrOculusPlugin, () => false, null),
                new Entry(QualityScope.Global, InclusiveMode.XRManagement, Style.vrSinglePassInstancing, () => false, null),
                new Entry(QualityScope.Global, InclusiveMode.VR, Style.vrLegacyHelpersPackage, IsVRLegacyHelpersCorrect, FixVRLegacyHelpers)
            });

            var currentBuildTarget = CalculateSelectedBuildTarget();
            if (( currentBuildTarget == BuildTarget.PS5) || (currentBuildTarget == BuildTarget.GameCoreXboxSeries ))
            {
                entryList.AddRange(new[]
                {
                    new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrAutoGraphicsAPIWarning_WindowsOnly, IsDXRAutoGraphicsAPICorrect_WindowsOnly, FixDXRAutoGraphicsAPI_WindowsOnly),
                    new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrD3D12Warning_WindowsOnly, IsDXRDirect3D12Correct_WindowsOnly, FixDXRDirect3D12_WindowsOnly),
                    new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrStaticBatching, IsDXRStaticBatchingCorrect, FixDXRStaticBatching),
                    new Entry(QualityScope.CurrentQuality, InclusiveMode.DXR, Style.dxrActivated, IsDXRActivationCorrect, FixDXRActivation),
                    new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrBuildTarget, IsValidBuildTarget, FixBuildTarget),
                    new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrResources, IsDXRResourcesCorrect, FixDXRResources),
                });
            }
            else
            {
                entryList.AddRange(new[]
                {
                    new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrAutoGraphicsAPI, IsDXRAutoGraphicsAPICorrect, FixDXRAutoGraphicsAPI),
                    new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrD3D12, IsDXRDirect3D12Correct, FixDXRDirect3D12),
                    new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrStaticBatching, IsDXRStaticBatchingCorrect, FixDXRStaticBatching),
                    new Entry(QualityScope.CurrentQuality, InclusiveMode.DXR, Style.dxrActivated, IsDXRActivationCorrect, FixDXRActivation),
                    new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrBuildTarget, IsValidBuildTarget, FixBuildTarget),
                    new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrResources, IsDXRResourcesCorrect, FixDXRResources),
                });
            }


            // Add the Optional checks
            entryList.AddRange(new[]
            {
                new Entry(QualityScope.CurrentQuality, InclusiveMode.DXROptional, Style.dxrScreenSpaceShadow, IsDXRScreenSpaceShadowCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: true),
                new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrScreenSpaceShadowFS, IsDXRScreenSpaceShadowFSCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: false),
                new Entry(QualityScope.CurrentQuality, InclusiveMode.DXROptional, Style.dxrReflections, IsDXRReflectionsCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: true),
                new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrReflectionsFS, IsDXRReflectionsFSCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: false),
                new Entry(QualityScope.CurrentQuality, InclusiveMode.DXROptional, Style.dxrTransparentReflections, IsDXRTransparentReflectionsCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: true),
                new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrTransparentReflectionsFS, IsDXRTransparentReflectionsFSCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: false),
                new Entry(QualityScope.CurrentQuality, InclusiveMode.DXROptional, Style.dxrGI, IsDXRGICorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: true),
                new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrGIFS, IsDXRGIFSCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: false),
            });

            return entryList.ToArray();
        }

        internal void ReBuildEntryList()
        {
            m_Entries = BuildEntryList();
        }

        Entry[] m_Entries;
        //To add elements in the Wizard configuration checker,
        //add your new checks in this array at the right position.
        //Both "Fix All" button and UI drawing will use it.
        //Indentation is computed in Entry if you use certain subscope.
        Entry[] entries
        {
            get
            {
                // due to functor, cannot static link directly in an array and need lazy init
                if (m_Entries == null)
                    m_Entries = BuildEntryList();
                return m_Entries;
            }
        }

        // Utility that grab all check within the scope or in sub scope included and check if everything is correct
        bool IsAllEntryCorrectInScope(InclusiveMode scope)
        {
            IEnumerable<Entry.Checker> checks = entries.Where(e => scope.Contains(e.inclusiveScope)).Select(e => e.check);
            if (checks.Count() == 0)
                return true;

            IEnumerator<Entry.Checker> enumerator = checks.GetEnumerator();
            enumerator.MoveNext();
            bool result = enumerator.Current();
            if (enumerator.MoveNext())
                for (; result && enumerator.MoveNext();)
                    result &= enumerator.Current();
            return result;
        }

        // Utility that grab all check and fix within the scope or in sub scope included and performe fix if check return incorrect
        void FixAllEntryInScope(InclusiveMode scope)
        {
            IEnumerable<(Entry.Checker, Entry.Fixer)> pairs = entries.Where(e => scope.Contains(e.inclusiveScope)).Select(e => (e.check, e.fix));
            if (pairs.Count() == 0)
                return;

            foreach ((Entry.Checker check, Entry.Fixer fix) in pairs)
                if (fix != null)
                    m_Fixer.Add(() =>
                    {
                        if (!check())
                            fix(fromAsync: true);
                    });
        }

        #endregion

        #region Queue

        class QueuedLauncher
        {
            Queue<Action> m_Queue = new Queue<Action>();
            bool m_Running = false;
            bool m_StopRequested = false;
            bool m_OnPause = false;

            public void Stop() => m_StopRequested = true;

            // Function to pause/unpause the action execution
            public void Pause() => m_OnPause = true;
            public void Unpause() => m_OnPause = false;

            public int remainingFixes => m_Queue.Count;

            void Start()
            {
                m_Running = true;
                EditorApplication.update += Run;
            }

            void End()
            {
                EditorApplication.update -= Run;
                m_Running = false;
            }

            void Run()
            {
                if (m_StopRequested)
                {
                    m_Queue.Clear();
                    m_StopRequested = false;
                }
                if (m_Queue.Count > 0)
                {
                    if (!m_OnPause)
                    {
                        m_Queue.Dequeue()?.Invoke();
                    }
                }
                else
                    End();
            }

            public void Add(Action function)
            {
                m_Queue.Enqueue(function);
                if (!m_Running)
                    Start();
            }

            public void Add(params Action[] functions)
            {
                foreach (Action function in functions)
                    Add(function);
            }
        }
        QueuedLauncher m_Fixer = new QueuedLauncher();

        void RestartFixAllAfterDomainReload()
        {
            if (m_Fixer.remainingFixes > 0)
                HDUserSettings.wizardNeedToRunFixAllAgainAfterDomainReload = true;
        }

        void CheckPersistentFixAll()
        {
            if (HDUserSettings.wizardNeedToRunFixAllAgainAfterDomainReload)
            {
                switch ((Configuration)HDUserSettings.wizardActiveTab)
                {
                    case Configuration.HDRP:
                        FixHDRPAll();
                        break;
                    case Configuration.HDRP_VR:
                        FixVRAll();
                        break;
                    case Configuration.HDRP_DXR:
                        FixDXRAll();
                        break;
                }
                m_Fixer.Add(() => HDUserSettings.wizardNeedToRunFixAllAgainAfterDomainReload = false);
            }
        }

        #endregion

        #region HDRP_FIXES

        bool IsHDRPAllCorrect()
            => IsAllEntryCorrectInScope(InclusiveMode.HDRP);

        void FixHDRPAll()
            => FixAllEntryInScope(InclusiveMode.HDRP);

        bool IsColorSpaceCorrect()
            => PlayerSettings.colorSpace == ColorSpace.Linear;

        void FixColorSpace(bool fromAsyncUnused)
            => PlayerSettings.colorSpace = ColorSpace.Linear;

        bool IsLightmapCorrect()
        {
            // Shame alert: plateform supporting Encodement are partly hardcoded
            // in editor (Standalone) and for the other part, it is all in internal code.
            return GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Standalone) == LightmapEncodingQualityCopy.High
                && GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android) == LightmapEncodingQualityCopy.High
                && GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.WSA) == LightmapEncodingQualityCopy.High;
        }

        void FixLightmap(bool fromAsyncUnused)
        {
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Standalone, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.WSA, LightmapEncodingQualityCopy.High);
        }

        bool IsShadowCorrect()
            => QualitySettings.shadows == ShadowQuality.All;

        void FixShadow(bool fromAsyncUnised)
        {
            QualitySettings.ForEach(() => QualitySettings.shadows = ShadowQuality.All);
        }

        bool IsShadowmaskCorrect()
            => QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask;

        void FixShadowmask(bool fromAsyncUnused)
        {
            QualitySettings.ForEach(() => QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask);
        }

        // To be removed as soon as GraphicsSettings.renderPipelineAsset is removed
        bool IsHdrpAssetGraphicsUsedCorrect()
            => GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset;

        void FixHdrpAssetGraphicsUsed(bool fromAsync)
        {
            if (ObjectSelector.opened)
                return;
            CreateOrLoad<HDRenderPipelineAsset>(fromAsync
                ? () => m_Fixer.Stop()
                : (Action)null,
                asset => GraphicsSettings.renderPipelineAsset = asset);
        }

        bool IsHdrpAssetQualityUsedCorrect()
            => QualitySettings.renderPipeline == null || QualitySettings.renderPipeline is HDRenderPipelineAsset;

        void FixHdrpAssetQualityUsed(bool fromAsync)
            => QualitySettings.renderPipeline = null;

        bool IsHdrpGlobalSettingsUsedCorrect()
            => HDRenderPipelineGlobalSettings.instance != null;

        void FixHdrpGlobalSettingsUsed(bool fromAsync)
            => HDRenderPipelineGlobalSettings.Ensure();

        bool IsRuntimeResourcesCorrect()
            => IsHdrpGlobalSettingsUsedCorrect() && HDRenderPipelineGlobalSettings.instance.AreRuntimeResourcesCreated();

        void FixRuntimeResources(bool fromAsyncUnused)
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                FixHdrpGlobalSettingsUsed(fromAsync: false);

            HDRenderPipelineGlobalSettings.instance.EnsureRuntimeResources(forceReload: true);
        }

        bool IsEditorResourcesCorrect()
            => IsHdrpGlobalSettingsUsedCorrect() && HDRenderPipelineGlobalSettings.instance.AreEditorResourcesCreated();

        void FixEditorResources(bool fromAsyncUnused)
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                FixHdrpGlobalSettingsUsed(fromAsync: false);

            HDRenderPipelineGlobalSettings.instance.EnsureEditorResources(forceReload: true);
        }

        bool IsSRPBatcherCorrect()
            => IsHdrpAssetQualityUsedCorrect() && (HDRenderPipeline.currentAsset?.enableSRPBatcher ?? false);

        void FixSRPBatcher(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetQualityUsedCorrect())
                FixHdrpAssetQualityUsed(fromAsync: false);

            var hdrpAsset = HDRenderPipeline.currentAsset;
            if (hdrpAsset == null)
                return;

            hdrpAsset.enableSRPBatcher = true;
            EditorUtility.SetDirty(hdrpAsset);
        }

        bool IsDiffusionProfileCorrect()
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                return false;

            var profileList = HDRenderPipelineGlobalSettings.instance.diffusionProfileSettingsList;
            return profileList.Length != 0 && profileList.Any(p => p != null);
        }

        void FixDiffusionProfile(bool fromAsyncUnused)
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                FixHdrpGlobalSettingsUsed(fromAsync: false);

            if (!IsEditorResourcesCorrect())
                FixEditorResources(fromAsyncUnused: false);

            if (!IsDefaultVolumeProfileCorrect())
                FixDefaultVolumeProfile(fromAsyncUnused: false);

            var defaultAssetList = HDRenderPipelineGlobalSettings.instance.renderPipelineEditorResources.defaultDiffusionProfileSettingsList;
            HDRenderPipelineGlobalSettings.instance.diffusionProfileSettingsList = new DiffusionProfileSettings[0]; // clear the diffusion profile list

            foreach (var diffusionProfileAsset in defaultAssetList)
            {
                HDRenderPipelineGlobalSettings.instance.AddDiffusionProfile((DiffusionProfileSettings)diffusionProfileAsset);
            }

            EditorUtility.SetDirty(HDRenderPipelineGlobalSettings.instance);
        }

        VolumeProfile CreateDefaultVolumeProfileIfNeeded(VolumeProfile defaultSettingsVolumeProfileInPackage)
        {
            string defaultSettingsVolumeProfilePath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + '/' + defaultSettingsVolumeProfileInPackage.name + ".asset";

            if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

            //try load one if one already exist
            VolumeProfile defaultSettingsVolumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultSettingsVolumeProfilePath);
            if (defaultSettingsVolumeProfile == null || defaultSettingsVolumeProfile.Equals(null))
            {
                //else create it
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(defaultSettingsVolumeProfileInPackage), defaultSettingsVolumeProfilePath);
                defaultSettingsVolumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultSettingsVolumeProfilePath);
            }

            return defaultSettingsVolumeProfile;
        }

        bool IsDefaultVolumeProfileCorrect()
            => IsHdrpGlobalSettingsUsedCorrect() && !HDRenderPipelineGlobalSettings.instance.IsVolumeProfileFromResources();

        void FixDefaultVolumeProfile(bool fromAsyncUnused)
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                FixHdrpGlobalSettingsUsed(fromAsync: false);

            if (!IsEditorResourcesCorrect())
                FixEditorResources(fromAsyncUnused: false);

            var hdrpSettings = HDRenderPipelineGlobalSettings.instance;
            hdrpSettings.volumeProfile = CreateDefaultVolumeProfileIfNeeded(hdrpSettings.renderPipelineEditorResources.defaultSettingsVolumeProfile);

            EditorUtility.SetDirty(hdrpSettings);
        }

        bool IsDefaultLookDevVolumeProfileCorrect()
            => IsHdrpGlobalSettingsUsedCorrect() && !HDRenderPipelineGlobalSettings.instance.IsVolumeProfileLookDevFromResources();

        void FixDefaultLookDevVolumeProfile(bool fromAsyncUnused)
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                FixHdrpGlobalSettingsUsed(fromAsync: false);

            if (!IsEditorResourcesCorrect())
                FixEditorResources(fromAsyncUnused: false);

            var hdrpSettings = HDRenderPipelineGlobalSettings.instance;
            hdrpSettings.lookDevVolumeProfile = CreateDefaultVolumeProfileIfNeeded(hdrpSettings.renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile);

            EditorUtility.SetDirty(hdrpSettings);
        }

        IEnumerable<IMigratableAsset> migratableAssets
        {
            get
            {
                // Note: ideally we should grab all migratableAssets with LoadAllAsset<IMigratableAsset but this can be at high cost for big project. So we check only currently used.
                //construct it wih all assets used in quality followed by current global settings asset
                List<IMigratableAsset> collection = new List<IMigratableAsset>();
                for (int i = QualitySettings.names.Length - 1; i >= 0; --i)
                {
                    if (QualitySettings.GetRenderPipelineAssetAt(i) is HDRenderPipelineAsset qualityAsset)
                        collection.Add(qualityAsset);
                }
                if (GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset graphicsAsset)
                    collection.Add(graphicsAsset);
                if (HDRenderPipelineGlobalSettings.instance)
                {
                    collection.Add(HDRenderPipelineGlobalSettings.instance.renderPipelineResources); //only resource that have migration
                    collection.Add(HDRenderPipelineGlobalSettings.instance);
                }
                return collection;
            }
        }

        bool IsMigratableAssetsCorrect()
            => !migratableAssets.Any(asset => !asset.IsAtLastVersion());

        void FixMigratableAssets(bool fromAsyncUnused)
        {
            foreach (var asset in migratableAssets)
            {
                if (asset.Migrate())
                    Debug.LogWarning($"Migrated asset {AssetDatabase.GetAssetPath(asset as UnityEngine.Object)}. You should save your project to save changes.");
            }
        }

        #endregion

        #region HDRP_VR_FIXES

        bool IsVRAllCorrect()
            => IsAllEntryCorrectInScope(InclusiveMode.VR);

        void FixVRAll()
            => FixAllEntryInScope(InclusiveMode.VR);

        bool IsOldVRSystemForCurrentBuildTargetGroupCorrect()
            => !VREditor.GetVREnabledOnTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        void FixOldVRSystemForCurrentBuildTargetGroup(bool fromAsyncUnused)
            => VREditor.SetVREnabledOnTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup, false);

        bool vrXRManagementInstalledCheck = false;
        bool IsVRXRManagementPackageInstalledCorrect()
        {
            m_UsedPackageRetriever.ProcessAsync(
                k_XRanagementPackageName,
                (installed, info) => vrXRManagementInstalledCheck = installed);
            return vrXRManagementInstalledCheck;
        }

        void FixVRXRManagementPackageInstalled(bool fromAsync)
        {
            if (fromAsync)
                RestartFixAllAfterDomainReload();
            m_PackageInstaller.ProcessAsync(k_XRanagementPackageName, null);
        }

        bool vrLegacyHelpersInstalledCheck = false;
        bool IsVRLegacyHelpersCorrect()
        {
            m_UsedPackageRetriever.ProcessAsync(
                k_LegacyInputHelpersPackageName,
                (installed, info) => vrLegacyHelpersInstalledCheck = installed);
            return vrLegacyHelpersInstalledCheck;
        }

        void FixVRLegacyHelpers(bool fromAsync)
        {
            if (fromAsync)
                RestartFixAllAfterDomainReload();
            m_PackageInstaller.ProcessAsync(k_LegacyInputHelpersPackageName, null);
        }

        #endregion

        #region HDRP_DXR_FIXES

        bool IsDXRAllCorrect()
            => IsAllEntryCorrectInScope(InclusiveMode.DXR);

        void FixDXRAll()
            => FixAllEntryInScope(InclusiveMode.DXR);

        bool IsDXRAutoGraphicsAPICorrect_WindowsOnly()
            => !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64) && !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows);

        void FixDXRAutoGraphicsAPI_WindowsOnly(bool fromAsyncUnused)
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows, false);
        }

        bool IsDXRAutoGraphicsAPICorrect()
            => (!PlayerSettings.GetUseDefaultGraphicsAPIs(CalculateSelectedBuildTarget()));

        void FixDXRAutoGraphicsAPI(bool fromAsyncUnused)
            => PlayerSettings.SetUseDefaultGraphicsAPIs(CalculateSelectedBuildTarget(), false);

        bool IsDXRDirect3D12Correct()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 && !HDUserSettings.wizardNeedRestartAfterChangingToDX12;
        }

        void FixDXRDirect3D12(bool fromAsyncUnused)
        {
            if (GetSupportedGraphicsAPIs(CalculateSelectedBuildTarget()).Contains(GraphicsDeviceType.Direct3D12))
            {
                var buidTarget = CalculateSelectedBuildTarget();
                if (PlayerSettings.GetGraphicsAPIs(buidTarget).Contains(GraphicsDeviceType.Direct3D12))
                {
                    PlayerSettings.SetGraphicsAPIs(
                        buidTarget,
                        new[] { GraphicsDeviceType.Direct3D12 }
                            .Concat(
                            PlayerSettings.GetGraphicsAPIs(buidTarget)
                                .Where(x => x != GraphicsDeviceType.Direct3D12))
                            .ToArray());
                }
                else
                {
                    PlayerSettings.SetGraphicsAPIs(
                        buidTarget,
                        new[] { GraphicsDeviceType.Direct3D12 }
                            .Concat(PlayerSettings.GetGraphicsAPIs(buidTarget))
                            .ToArray());
                }
                HDUserSettings.wizardNeedRestartAfterChangingToDX12 = true;
                m_Fixer.Add(() => ChangedFirstGraphicAPI(buidTarget)); //register reboot at end of operations
            }
        }

        bool IsDXRDirect3D12Correct_WindowsOnly()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 && !HDUserSettings.wizardNeedRestartAfterChangingToDX12;
        }

        void FixDXRDirect3D12_WindowsOnly(bool fromAsyncUnused)
        {
            if (PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).Contains(GraphicsDeviceType.Direct3D12))
            {
                PlayerSettings.SetGraphicsAPIs(
                    BuildTarget.StandaloneWindows64,
                    new[] { GraphicsDeviceType.Direct3D12 }
                        .Concat(
                        PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64)
                            .Where(x => x != GraphicsDeviceType.Direct3D12))
                        .ToArray());
            }
            else
            {
                PlayerSettings.SetGraphicsAPIs(
                    BuildTarget.StandaloneWindows64,
                    new[] { GraphicsDeviceType.Direct3D12 }
                        .Concat(PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64))
                        .ToArray());
            }
            HDUserSettings.wizardNeedRestartAfterChangingToDX12 = true;
            m_Fixer.Add(() => ChangedFirstGraphicAPI(BuildTarget.StandaloneWindows64)); //register reboot at end of operations
        }

        void ChangedFirstGraphicAPI(BuildTarget target)
        {
            //It seams that the 64 version is not check for restart for a strange reason
            if (target == BuildTarget.StandaloneWindows64)
                target = BuildTarget.StandaloneWindows;

            // If we're changing the first API for relevant editor, this will cause editor to switch: ask for scene save & confirmation
            if (WillEditorUseFirstGraphicsAPI(target))
            {
                if (EditorUtility.DisplayDialog("Changing editor graphics device",
                    "You've changed the active graphics API. This requires a restart of the Editor. After restarting, finish fixing DXR configuration by launching the wizard again.",
                    "Restart Editor", "Not now"))
                {
                    HDUserSettings.wizardNeedRestartAfterChangingToDX12 = false;
                    RequestCloseAndRelaunchWithCurrentArguments();
                }
                else
                    EditorApplication.quitting += () => HDUserSettings.wizardNeedRestartAfterChangingToDX12 = false;
            }
        }

        void CheckPersistantNeedReboot()
        {
            if (HDUserSettings.wizardNeedRestartAfterChangingToDX12)
                EditorApplication.quitting += () => HDUserSettings.wizardNeedRestartAfterChangingToDX12 = false;
        }

        bool IsDXRResourcesCorrect()
        {
            var selectedBuildTarget = CalculateSelectedBuildTarget();
            return IsHdrpGlobalSettingsUsedCorrect()
                && HDRenderPipelineGlobalSettings.instance.AreRayTracingResourcesCreated()
                && (SystemInfo.supportsRayTracing || selectedBuildTarget == BuildTarget.GameCoreXboxSeries || selectedBuildTarget == BuildTarget.PS5);
        }

        void FixDXRResources(bool fromAsyncUnused)
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                FixHdrpGlobalSettingsUsed(fromAsync: false);

            if (SystemInfo.supportsRayTracing)
                HDRenderPipelineGlobalSettings.instance.EnsureRayTracingResources(forceReload: true);

            // IMPORTANT: We display the error only if we are D3D12 as the supportsRayTracing always return false in any other device even if OS/HW supports DXR.
            // The D3D12 is a separate check in the wizard, so it is fine not to display an error in case we are not D3D12.
            if (!SystemInfo.supportsRayTracing && IsDXRDirect3D12Correct())
                Debug.LogError("Your hardware and/or OS don't support DXR!");
            if (!HDUserSettings.wizardNeedRestartAfterChangingToDX12 && PlayerSettings.GetGraphicsAPIs(CalculateSelectedBuildTarget()).FirstOrDefault() != GraphicsDeviceType.Direct3D12)
            {
                Debug.LogWarning("DXR is supported only with DX12");
            }
        }

        bool IsDXRScreenSpaceShadowCorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows;

        bool IsDXRScreenSpaceShadowFSCorrect()
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                return false;

            FrameSettings defaultCameraFS = HDRenderPipelineGlobalSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
            return defaultCameraFS.IsEnabled(FrameSettingsField.ScreenSpaceShadows);
        }

        bool IsDXRReflectionsCorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportSSR;

        bool IsDXRReflectionsFSCorrect()
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                return false;

            FrameSettings defaultCameraFS = HDRenderPipelineGlobalSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
            return defaultCameraFS.IsEnabled(FrameSettingsField.SSR);
        }

        bool IsDXRTransparentReflectionsCorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportSSRTransparent;

        bool IsDXRTransparentReflectionsFSCorrect()
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                return false;

            FrameSettings defaultCameraFS = HDRenderPipelineGlobalSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
            return defaultCameraFS.IsEnabled(FrameSettingsField.TransparentSSR);
        }

        bool IsDXRGICorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportSSGI;

        bool IsDXRGIFSCorrect()
        {
            if (!IsHdrpGlobalSettingsUsedCorrect())
                return false;

            FrameSettings defaultCameraFS = HDRenderPipelineGlobalSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
            return defaultCameraFS.IsEnabled(FrameSettingsField.SSGI);
        }

        bool IsValidBuildTarget()
        {
            return (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
                || (EditorUserBuildSettings.activeBuildTarget == BuildTarget.GameCoreXboxSeries)
                || (EditorUserBuildSettings.activeBuildTarget == BuildTarget.PS5);
        }

        void FixBuildTarget(bool fromAsyncUnused)
        {
            if ((EditorUserBuildSettings.activeBuildTarget != BuildTarget.PS5) && (EditorUserBuildSettings.activeBuildTarget != BuildTarget.GameCoreXboxSeries))
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }

        bool IsDXRStaticBatchingCorrect()
            => !GetStaticBatching(CalculateSelectedBuildTarget());

        void FixDXRStaticBatching(bool fromAsyncUnused)
        {
            SetStaticBatching(CalculateSelectedBuildTarget(), false);
        }

        bool IsDXRActivationCorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportRayTracing;

        void FixDXRActivation(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetQualityUsedCorrect())
                FixHdrpAssetQualityUsed(fromAsync: false);

            //as property returning struct make copy, use serializedproperty to modify it
            var serializedObject = new SerializedObject(HDRenderPipeline.currentAsset);
            var propertySupportRayTracing = serializedObject.FindProperty("m_RenderPipelineSettings.supportRayTracing");
            propertySupportRayTracing.boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Package Manager

        const string k_HdrpPackageName = "com.unity.render-pipelines.high-definition";
        const string k_HdrpConfigPackageName = "com.unity.render-pipelines.high-definition-config";
        const string k_XRanagementPackageName = "com.unity.xr.management";
        const string k_LegacyInputHelpersPackageName = "com.unity.xr.legacyinputhelpers";

        void IsLocalConfigurationPackageEmbeddedAsync(Action<bool> callback)
        {
            WaitForRequest(PackageManager.Client.List(true, true), listRequest =>
            {
                if (listRequest.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"Package Manager error: {listRequest.Error.message}");
                    return;
                }

                var packageInfo = listRequest.Result.First(info => info.name == k_HdrpConfigPackageName);
                var isPackagedEmbedded = packageInfo?.source == PackageSource.Embedded;
                callback?.Invoke(isPackagedEmbedded);
            });
        }


        void EmbedConfigPackage(bool installed, string name, Action onCompletion)
        {
            if (!installed)
            {
                Debug.LogError("The the HDRP config package is missing, please install the one with the same version of your HDRP package.");
                return;
            }

            WaitForRequest(Client.Embed(name), embedRequest =>
            {
                if (embedRequest.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"Failed to install the config package {embedRequest.Error.message}");
                    return;
                }

                onCompletion?.Invoke();
            });
        }

        void InstallLocalConfigurationPackage(Action onCompletion)
        {
            m_UsedPackageRetriever.ProcessAsync(
            k_HdrpConfigPackageName,
            (installed, info) =>
            {
                // Embedding a package requires it to be an explicit direct dependency in the manifest.
                // If it's not, we add it first.
                if (!info.isDirectDependency)
                {
                    m_PackageInstaller.ProcessAsync(k_HdrpConfigPackageName, () => m_UsedPackageRetriever.ProcessAsync(
                        k_HdrpConfigPackageName,
                        (installed, info) =>
                        {
                            EmbedConfigPackage(installed, info.name, onCompletion);

                        }));
                }
                else
                {
                    EmbedConfigPackage(installed, info.name, onCompletion);
                }
            });
        }


        static void WaitForRequest<T>(T request, Action<T> onCompleted)
            where T : Request
        {
            if (request.IsCompleted)
                onCompleted(request);
            else
                EditorApplication.delayCall += () => WaitForRequest<T>(request, onCompleted);
        }

        void RefreshDisplayOfConfigPackageArea()
        {
            IsLocalConfigurationPackageEmbeddedAsync(present => UpdateDisplayOfConfigPackageArea(present ? ConfigPackageState.Present : ConfigPackageState.Missing));
        }

        class UsedPackageRetriever
        {
            PackageManager.Requests.ListRequest m_CurrentRequest;
            Action<bool, PackageManager.PackageInfo> m_CurrentAction;
            string m_CurrentPackageName;

            Queue<(string packageName, Action<bool, PackageManager.PackageInfo> action)> m_Queue = new Queue<(string packageName, Action<bool, PackageManager.PackageInfo> action)>();

            bool isCurrentInProgress => m_CurrentRequest != null && !m_CurrentRequest.Equals(null) && !m_CurrentRequest.IsCompleted;

            public bool isRunning => isCurrentInProgress || m_Queue.Count() > 0;

            public void ProcessAsync(string packageName, Action<bool, PackageManager.PackageInfo> action)
            {
                if (isCurrentInProgress)
                    m_Queue.Enqueue((packageName, action));
                else
                    Start(packageName, action);
            }

            void Start(string packageName, Action<bool, PackageManager.PackageInfo> action)
            {
                m_CurrentAction = action;
                m_CurrentPackageName = packageName;
                m_CurrentRequest = PackageManager.Client.List(offlineMode: true, includeIndirectDependencies: true);
                EditorApplication.update += Progress;
            }

            void Progress()
            {
                //Can occures on Wizard close or if scripts reloads
                if (m_CurrentRequest == null || m_CurrentRequest.Equals(null))
                {
                    EditorApplication.update -= Progress;
                    return;
                }

                if (m_CurrentRequest.IsCompleted)
                    Finished();
            }

            void Finished()
            {
                EditorApplication.update -= Progress;
                if (m_CurrentRequest.Status == PackageManager.StatusCode.Success)
                {
                    var filteredResults = m_CurrentRequest.Result.Where(info => info.name == m_CurrentPackageName);
                    if (filteredResults.Count() == 0)
                        m_CurrentAction?.Invoke(false, default);
                    else
                    {
                        PackageManager.PackageInfo result = filteredResults.First();
                        m_CurrentAction?.Invoke(true, result);
                    }
                }
                else if (m_CurrentRequest.Status >= PackageManager.StatusCode.Failure)
                    Debug.LogError($"Failed to find package {m_CurrentPackageName}. Reason: {m_CurrentRequest.Error.message}");
                else
                    Debug.LogError("Unsupported progress state " + m_CurrentRequest.Status);

                m_CurrentRequest = null;

                if (m_Queue.Count > 0)
                {
                    (string packageIdOrName, Action<bool, PackageManager.PackageInfo> action) = m_Queue.Dequeue();
                    EditorApplication.delayCall += () => Start(packageIdOrName, action);
                }
            }
        }
        UsedPackageRetriever m_UsedPackageRetriever = new UsedPackageRetriever();

        class PackageInstaller
        {
            PackageManager.Requests.AddRequest m_CurrentRequest;
            Action m_CurrentAction;
            string m_CurrentPackageName;

            Queue<(string packageName, Action action)> m_Queue = new Queue<(string packageName, Action action)>();

            bool isCurrentInProgress => m_CurrentRequest != null && !m_CurrentRequest.Equals(null) && !m_CurrentRequest.IsCompleted;

            public bool isRunning => isCurrentInProgress || m_Queue.Count() > 0;

            public void ProcessAsync(string packageName, Action action)
            {
                if (isCurrentInProgress)
                    m_Queue.Enqueue((packageName, action));
                else
                    Start(packageName, action);
            }

            void Start(string packageName, Action action)
            {
                m_CurrentAction = action;
                m_CurrentPackageName = packageName;
                m_CurrentRequest = PackageManager.Client.Add(packageName);
                EditorApplication.update += Progress;
            }

            void Progress()
            {
                //Can occur on Wizard close or if scripts reloads
                if (m_CurrentRequest == null || m_CurrentRequest.Equals(null))
                {
                    EditorApplication.update -= Progress;
                    return;
                }

                if (m_CurrentRequest.IsCompleted)
                    Finished();
            }

            void Finished()
            {
                EditorApplication.update -= Progress;
                if (m_CurrentRequest.Status == PackageManager.StatusCode.Success)
                {
                    m_CurrentAction?.Invoke();
                }
                else if (m_CurrentRequest.Status >= PackageManager.StatusCode.Failure)
                    Debug.LogError($"Failed to find package {m_CurrentPackageName}. Reason: {m_CurrentRequest.Error.message}");
                else
                    Debug.LogError("Unsupported progress state " + m_CurrentRequest.Status);

                m_CurrentRequest = null;

                if (m_Queue.Count > 0)
                {
                    (string packageIdOrName, Action action) = m_Queue.Dequeue();
                    EditorApplication.delayCall += () => Start(packageIdOrName, action);
                }
            }
        }
        PackageInstaller m_PackageInstaller = new PackageInstaller();
        #endregion
    }
}
