using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal.VR;
using UnityEditor.SceneManagement;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using UnityEditorInternal;

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

    partial class HDWizard
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

        //To add elements in the Wizard configuration checker,
        //add your new checks in this array at the right position.
        //Both "Fix All" button and UI drawing will use it.
        //Indentation is computed in Entry if you use certain subscope.
        Entry[] m_Entries;
        Entry[] entries
        {
            get
            {
                // due to functor, cannot static link directly in an array and need lazy init
                if (m_Entries == null)
                    m_Entries = new[]
                    {
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpColorSpace, IsColorSpaceCorrect, FixColorSpace),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpLightmapEncoding, IsLightmapCorrect, FixLightmap),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpShadow, IsShadowCorrect, FixShadow),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpShadowmask, IsShadowmaskCorrect, FixShadowmask),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpAssetGraphicsAssigned, IsHdrpAssetGraphicsUsedCorrect, FixHdrpAssetGraphicsUsed),
                        new Entry(QualityScope.CurrentQuality, InclusiveMode.HDRP, Style.hdrpAssetQualityAssigned, IsHdrpAssetQualityUsedCorrect, FixHdrpAssetQualityUsed),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpAssetRuntimeResources, IsHdrpAssetRuntimeResourcesCorrect, FixHdrpAssetRuntimeResources),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpAssetEditorResources, IsHdrpAssetEditorResourcesCorrect, FixHdrpAssetEditorResources),
                        new Entry(QualityScope.CurrentQuality, InclusiveMode.HDRP, Style.hdrpBatcher, IsSRPBatcherCorrect, FixSRPBatcher),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpAssetDiffusionProfile, IsHdrpAssetDiffusionProfileCorrect, FixHdrpAssetDiffusionProfile),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpVolumeProfile, IsDefaultVolumeProfileAssigned, FixDefaultVolumeProfileAssigned),
                        new Entry(QualityScope.Global, InclusiveMode.HDRP, Style.hdrpLookDevVolumeProfile, IsDefaultLookDevVolumeProfileAssigned, FixDefaultLookDevVolumeProfileAssigned),

                        new Entry(QualityScope.Global, InclusiveMode.VR, Style.vrLegacyVRSystem, IsOldVRSystemForCurrentBuildTargetGroupCorrect, FixOldVRSystemForCurrentBuildTargetGroup),
                        new Entry(QualityScope.Global, InclusiveMode.VR, Style.vrXRManagementPackage, IsVRXRManagementPackageInstalledCorrect, FixVRXRManagementPackageInstalled),
                        new Entry(QualityScope.Global, InclusiveMode.XRManagement, Style.vrOculusPlugin, () => false, null),
                        new Entry(QualityScope.Global, InclusiveMode.XRManagement, Style.vrSinglePassInstancing, () => false, null),
                        new Entry(QualityScope.Global, InclusiveMode.VR, Style.vrLegacyHelpersPackage, IsVRLegacyHelpersCorrect, FixVRLegacyHelpers),

                        new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrAutoGraphicsAPI, IsDXRAutoGraphicsAPICorrect, FixDXRAutoGraphicsAPI),
                        new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrD3D12, IsDXRDirect3D12Correct, FixDXRDirect3D12),
                        new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrStaticBatching, IsDXRStaticBatchingCorrect, FixDXRStaticBatching),
                        new Entry(QualityScope.CurrentQuality, InclusiveMode.DXR, Style.dxrActivated, IsDXRActivationCorrect, FixDXRActivation),
                        new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxr64bits, IsArchitecture64Bits, FixArchitecture64Bits),
                        new Entry(QualityScope.Global, InclusiveMode.DXR, Style.dxrResources, IsDXRAssetCorrect, FixDXRAsset),

                        // Optional checks
                        new Entry(QualityScope.CurrentQuality, InclusiveMode.DXROptional, Style.dxrScreenSpaceShadow, IsDXRScreenSpaceShadowCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: true),
                        new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrScreenSpaceShadowFS, IsDXRScreenSpaceShadowFSCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: false),
                        new Entry(QualityScope.CurrentQuality, InclusiveMode.DXROptional, Style.dxrReflections, IsDXRReflectionsCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: true),
                        new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrReflectionsFS, IsDXRReflectionsFSCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: false),
                        new Entry(QualityScope.CurrentQuality, InclusiveMode.DXROptional, Style.dxrTransparentReflections, IsDXRTransparentReflectionsCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: true),
                        new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrTransparentReflectionsFS, IsDXRTransparentReflectionsFSCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: false),
                        new Entry(QualityScope.CurrentQuality, InclusiveMode.DXROptional, Style.dxrGI, IsDXRGICorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: true),
                        new Entry(QualityScope.Global, InclusiveMode.DXROptional, Style.dxrGIFS, IsDXRGIFSCorrect, null, forceDisplayCheck: true, skipErrorIcon: true, displayAssetName: false),
                    };
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
                HDProjectSettings.wizardNeedToRunFixAllAgainAfterDomainReload = true;
        }

        void CheckPersistentFixAll()
        {
            if (HDProjectSettings.wizardNeedToRunFixAllAgainAfterDomainReload)
            {
                switch ((Configuration)HDProjectSettings.wizardActiveTab)
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
                m_Fixer.Add(() => HDProjectSettings.wizardNeedToRunFixAllAgainAfterDomainReload = false);
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
                && GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Lumin) == LightmapEncodingQualityCopy.High
                && GetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.WSA) == LightmapEncodingQualityCopy.High;
        }

        void FixLightmap(bool fromAsyncUnused)
        {
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Standalone, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Lumin, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.WSA, LightmapEncodingQualityCopy.High);
        }

        bool IsShadowCorrect()
            => QualitySettings.shadows == ShadowQuality.All;

        void FixShadow(bool fromAsyncUnised)
        {
            int currentQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.shadows = ShadowQuality.All;
            }
            QualitySettings.SetQualityLevel(currentQuality, applyExpensiveChanges: false);
        }

        bool IsShadowmaskCorrect()
            => QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask;

        void FixShadowmask(bool fromAsyncUnused)
        {
            int currentQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
            }
            QualitySettings.SetQualityLevel(currentQuality, applyExpensiveChanges: false);
        }

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

        bool IsHdrpAssetRuntimeResourcesCorrect()
            => IsHdrpAssetGraphicsUsedCorrect()
            && HDRenderPipeline.defaultAsset.renderPipelineResources != null;

        void FixHdrpAssetRuntimeResources(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetGraphicsUsedCorrect())
                FixHdrpAssetGraphicsUsed(fromAsync: false);

            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset == null)
                return;

            var runtimeResourcesPath = HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset";
            var objs = InternalEditorUtility.LoadSerializedFileAndForget(runtimeResourcesPath);
            hdrpAsset.renderPipelineResources = objs != null && objs.Length > 0 ? objs.First() as RenderPipelineResources : null;
            if (ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineResources,
                HDUtils.GetHDRenderPipelinePath()))
            {
                InternalEditorUtility.SaveToSerializedFileAndForget(
                    new UnityEngine.Object[] { HDRenderPipeline.defaultAsset.renderPipelineResources },
                    runtimeResourcesPath,
                    true);
            }
        }

        bool IsHdrpAssetEditorResourcesCorrect()
            => IsHdrpAssetGraphicsUsedCorrect()
            && HDRenderPipeline.defaultAsset.renderPipelineEditorResources != null;

        void FixHdrpAssetEditorResources(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetGraphicsUsedCorrect())
                FixHdrpAssetGraphicsUsed(fromAsync: false);

            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset == null)
                return;

            hdrpAsset.renderPipelineEditorResources
                = AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset");
            ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineEditorResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsSRPBatcherCorrect()
            => IsHdrpAssetQualityUsedCorrect() && (HDRenderPipeline.currentAsset?.enableSRPBatcher ?? false);

        void FixSRPBatcher(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetQualityUsedCorrect())
                FixHdrpAssetQualityUsed(fromAsync: false);

            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset == null)
                return;

            hdrpAsset.enableSRPBatcher = true;
            EditorUtility.SetDirty(hdrpAsset);
        }

        bool IsHdrpAssetDiffusionProfileCorrect()
        {
            var profileList = HDRenderPipeline.defaultAsset?.diffusionProfileSettingsList;
            return IsHdrpAssetGraphicsUsedCorrect() && profileList.Length != 0 && profileList.Any(p => p != null);
        }

        void FixHdrpAssetDiffusionProfile(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetGraphicsUsedCorrect())
                FixHdrpAssetGraphicsUsed(fromAsync: false);

            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset == null)
                return;

            var defaultAssetList = hdrpAsset.renderPipelineEditorResources.defaultDiffusionProfileSettingsList;
            hdrpAsset.diffusionProfileSettingsList = new DiffusionProfileSettings[0]; // clear the diffusion profile list

            if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

            if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

            foreach (var diffusionProfileAsset in defaultAssetList)
            {
                string defaultDiffusionProfileSettingsPath = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + diffusionProfileAsset.name + ".asset";
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(diffusionProfileAsset), defaultDiffusionProfileSettingsPath);

                var userAsset = AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(defaultDiffusionProfileSettingsPath);
                hdrpAsset.AddDiffusionProfile(userAsset);
            }

            EditorUtility.SetDirty(hdrpAsset);
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
                if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                    AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);

                //else create it
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(defaultSettingsVolumeProfileInPackage), defaultSettingsVolumeProfilePath);
                defaultSettingsVolumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(defaultSettingsVolumeProfilePath);
            }

            return defaultSettingsVolumeProfile;
        }

        bool IsDefaultVolumeProfileAssigned()
        {
            if (!IsHdrpAssetGraphicsUsedCorrect())
                return false;

            var hdAsset = HDRenderPipeline.defaultAsset;
            return hdAsset.defaultVolumeProfile != null
                && !hdAsset.defaultVolumeProfile.Equals(null)
                && hdAsset.defaultVolumeProfile != hdAsset.renderPipelineEditorResources.defaultSettingsVolumeProfile;
        }

        void FixDefaultVolumeProfileAssigned(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetGraphicsUsedCorrect())
                FixHdrpAssetGraphicsUsed(fromAsync: false);

            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset == null)
                return;

            hdrpAsset.defaultVolumeProfile = CreateDefaultVolumeProfileIfNeeded(hdrpAsset.renderPipelineEditorResources.defaultSettingsVolumeProfile);

            EditorUtility.SetDirty(hdrpAsset);
        }

        bool IsDefaultLookDevVolumeProfileAssigned()
        {
            if (!IsHdrpAssetGraphicsUsedCorrect())
                return false;

            var hdAsset = HDRenderPipeline.defaultAsset;
            return hdAsset.defaultLookDevProfile != null
                && !hdAsset.defaultLookDevProfile.Equals(null)
                && hdAsset.defaultLookDevProfile != hdAsset.renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile;
        }

        void FixDefaultLookDevVolumeProfileAssigned(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetGraphicsUsedCorrect())
                FixHdrpAssetGraphicsUsed(fromAsync: false);

            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset == null)
                return;

            hdrpAsset.defaultLookDevProfile = CreateDefaultVolumeProfileIfNeeded(hdrpAsset.renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile);

            EditorUtility.SetDirty(hdrpAsset);
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

        bool IsDXRAutoGraphicsAPICorrect()
            => !PlayerSettings.GetUseDefaultGraphicsAPIs(CalculateSelectedBuildTarget());

        void FixDXRAutoGraphicsAPI(bool fromAsyncUnused)
            => PlayerSettings.SetUseDefaultGraphicsAPIs(CalculateSelectedBuildTarget(), false);

        bool IsDXRDirect3D12Correct()
            => (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12) && !HDProjectSettings.wizardNeedRestartAfterChangingToDX12;

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
                HDProjectSettings.wizardNeedRestartAfterChangingToDX12 = true;
                m_Fixer.Add(() => ChangedFirstGraphicAPI(buidTarget)); //register reboot at end of operations
            }
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
                    HDProjectSettings.wizardNeedRestartAfterChangingToDX12 = false;
                    RequestCloseAndRelaunchWithCurrentArguments();
                }
                else
                    EditorApplication.quitting += () => HDProjectSettings.wizardNeedRestartAfterChangingToDX12 = false;
            }
        }

        void CheckPersistantNeedReboot()
        {
            if (HDProjectSettings.wizardNeedRestartAfterChangingToDX12)
                EditorApplication.quitting += () => HDProjectSettings.wizardNeedRestartAfterChangingToDX12 = false;
        }

        bool IsDXRAssetCorrect()
            => HDRenderPipeline.defaultAsset != null
            && HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources != null
            && SystemInfo.supportsRayTracing;

        void FixDXRAsset(bool fromAsyncUnused)
        {
            if (!IsHdrpAssetGraphicsUsedCorrect())
                FixHdrpAssetGraphicsUsed(fromAsync: false);
            HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources
                = AssetDatabase.LoadAssetAtPath<HDRenderPipelineRayTracingResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRayTracingResources.asset");
            ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
            // IMPORTANT: We display the error only if we are D3D12 as the supportsRayTracing always return false in any other device even if OS/HW supports DXR.
            // The D3D12 is a separate check in the wizard, so it is fine not to display an error in case we are not D3D12.
            if (!SystemInfo.supportsRayTracing && IsDXRDirect3D12Correct())
                Debug.LogError("Your hardware and/or OS don't support DXR!");
            if (!HDProjectSettings.wizardNeedRestartAfterChangingToDX12 && PlayerSettings.GetGraphicsAPIs(CalculateSelectedBuildTarget()).FirstOrDefault() != GraphicsDeviceType.Direct3D12)
            {
                Debug.LogWarning("DXR is supported only with DX12");
            }
        }

        bool IsDXRScreenSpaceShadowCorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows;

        bool IsDXRScreenSpaceShadowFSCorrect()
        {
            HDRenderPipelineAsset hdrpAsset = HDRenderPipeline.defaultAsset; // Default FrameSettings is a global quality independent parameter
            if (hdrpAsset != null)
            {
                FrameSettings defaultCameraFS = hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
                return defaultCameraFS.IsEnabled(FrameSettingsField.ScreenSpaceShadows);
            }
            else
                return false;
        }

        bool IsDXRReflectionsCorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportSSR;

        bool IsDXRReflectionsFSCorrect()
        {
            HDRenderPipelineAsset hdrpAsset = HDRenderPipeline.defaultAsset; // Default FrameSettings is a global quality independent parameter
            if (hdrpAsset != null)
            {
                FrameSettings defaultCameraFS = hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
                return defaultCameraFS.IsEnabled(FrameSettingsField.SSR);
            }
            else
                return false;
        }

        bool IsDXRTransparentReflectionsCorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportSSRTransparent;

        bool IsDXRTransparentReflectionsFSCorrect()
        {
            HDRenderPipelineAsset hdrpAsset = HDRenderPipeline.defaultAsset; // Default FrameSettings is a global quality independent parameter
            if (hdrpAsset != null)
            {
                FrameSettings defaultCameraFS = hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
                return defaultCameraFS.IsEnabled(FrameSettingsField.TransparentSSR);
            }
            else
                return false;
        }

        bool IsDXRGICorrect()
            => HDRenderPipeline.currentAsset != null
            && HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportSSGI;

        bool IsDXRGIFSCorrect()
        {
            HDRenderPipelineAsset hdrpAsset = HDRenderPipeline.defaultAsset; // Default FrameSettings is a global quality independent parameter
            if (hdrpAsset != null)
            {
                FrameSettings defaultCameraFS = hdrpAsset.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
                return defaultCameraFS.IsEnabled(FrameSettingsField.SSGI);
            }
            else
                return false;
        }

        bool IsArchitecture64Bits()
            => EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64;

        void FixArchitecture64Bits(bool fromAsyncUnused)
        {
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
        const string k_LocalHdrpConfigPackagePath = "LocalPackages/com.unity.render-pipelines.high-definition-config";
        const string k_XRanagementPackageName = "com.unity.xr.management";
        const string k_LegacyInputHelpersPackageName = "com.unity.xr.legacyinputhelpers";

        bool lastPackageConfigInstalledCheck = false;
        void IsLocalConfigurationPackageInstalledAsync(Action<bool> callback)
        {
            if (!Directory.Exists(k_LocalHdrpConfigPackagePath))
            {
                callback?.Invoke(lastPackageConfigInstalledCheck = false);
                return;
            }

            m_UsedPackageRetriever.ProcessAsync(
                k_HdrpConfigPackageName,
                (installed, info) =>
                {
                    // installed is not used because this one will be always installed

                    DirectoryInfo directoryInfo = new DirectoryInfo(info.resolvedPath);
                    string recomposedPath = $"{directoryInfo.Parent.Name}{Path.DirectorySeparatorChar}{directoryInfo.Name}";
                    lastPackageConfigInstalledCheck =
                        info.source == PackageManager.PackageSource.Local
                        && info.resolvedPath.EndsWith(recomposedPath);
                    callback?.Invoke(lastPackageConfigInstalledCheck);
                });
        }

        void InstallLocalConfigurationPackage(Action onCompletion)
            => m_UsedPackageRetriever.ProcessAsync(
                k_HdrpConfigPackageName,
                (installed, info) =>
                {
                    // installed is not used because this one will be always installed

                    bool copyFolder = false;
                    if (!Directory.Exists(k_LocalHdrpConfigPackagePath))
                    {
                        copyFolder = true;
                    }
                    else
                    {
                        if (EditorUtility.DisplayDialog("Installing local configuration package",
                            "A local configuration package already exists. Do you want to replace it or keep it? Replacing it may overwrite local changes you made and keeping it may make it desynchronized with the main HDRP packages version.",
                            "Replace", "Keep"))
                        {
                            Directory.Delete(k_LocalHdrpConfigPackagePath, true);
                            copyFolder = true;
                        }
                    }

                    if (copyFolder)
                    {
                        CopyFolder(info.resolvedPath, k_LocalHdrpConfigPackagePath);
                    }

                    m_PackageInstaller.ProcessAsync($"file:../{k_LocalHdrpConfigPackagePath}", () =>
                    {
                        lastPackageConfigInstalledCheck = true;
                        onCompletion?.Invoke();
                    });
                });

        void RefreshDisplayOfConfigPackageArea()
        {
            IsLocalConfigurationPackageInstalledAsync(present => UpdateDisplayOfConfigPackageArea(present ? ConfigPackageState.Present : ConfigPackageState.Missing));
        }

        static void CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
                Directory.CreateDirectory(destFolder);
            string[] files = Directory.GetFiles(sourceFolder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest);
            }
            string[] folders = Directory.GetDirectories(sourceFolder);
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolder(folder, dest);
            }
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
