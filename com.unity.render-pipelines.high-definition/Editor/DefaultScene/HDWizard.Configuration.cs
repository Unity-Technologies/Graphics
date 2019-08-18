using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal.VR;
using UnityEditor.SceneManagement;
using System.Linq.Expressions;
using System.Reflection;

namespace UnityEditor.Rendering.HighDefinition
{
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

        static void LoadReflectionMethods()
        {
            Type playerSettingsType = typeof(PlayerSettings);
            Type playerSettingsEditorType = playerSettingsType.Assembly.GetType("UnityEditor.PlayerSettingsEditor");
            Type lightEncodingQualityType = playerSettingsType.Assembly.GetType("UnityEditor.LightmapEncodingQuality");
            Type editorUserBuildSettingsUtilsType = playerSettingsType.Assembly.GetType("UnityEditor.EditorUserBuildSettingsUtils");
            var qualityVariable = Expression.Variable(lightEncodingQualityType, "quality_internal");
            var buildTargetGroupParameter = Expression.Parameter(typeof(BuildTargetGroup), "platformGroup");
            var buildTargetParameter = Expression.Parameter(typeof(BuildTarget), "platform");
            var qualityParameter = Expression.Parameter(typeof(LightmapEncodingQualityCopy), "quality");
            var getLightmapEncodingQualityForPlatformGroupInfo = playerSettingsType.GetMethod("GetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            var setLightmapEncodingQualityForPlatformGroupInfo = playerSettingsType.GetMethod("SetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            var calculateSelectedBuildTargetInfo = editorUserBuildSettingsUtilsType.GetMethod("CalculateSelectedBuildTarget", BindingFlags.Static | BindingFlags.Public);
            var getSupportedGraphicsAPIsInfo = playerSettingsType.GetMethod("GetSupportedGraphicsAPIs", BindingFlags.Static | BindingFlags.NonPublic);
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
            var getLightmapEncodingQualityForPlatformGroupLambda = Expression.Lambda<Func<BuildTargetGroup, LightmapEncodingQualityCopy>>(getLightmapEncodingQualityForPlatformGroupBlock, buildTargetGroupParameter);
            var setLightmapEncodingQualityForPlatformGroupLambda = Expression.Lambda<Action<BuildTargetGroup, LightmapEncodingQualityCopy>>(setLightmapEncodingQualityForPlatformGroupBlock, buildTargetGroupParameter, qualityParameter);
            var calculateSelectedBuildTargetLambda = Expression.Lambda<Func<BuildTarget>>(Expression.Call(null, calculateSelectedBuildTargetInfo));
            var getSupportedGraphicsAPIsLambda = Expression.Lambda<Func<BuildTarget, GraphicsDeviceType[]>>(Expression.Call(null, getSupportedGraphicsAPIsInfo, buildTargetParameter), buildTargetParameter);
            var willEditorUseFirstGraphicsAPILambda = Expression.Lambda<Func<BuildTarget, bool>>(Expression.Call(null, willEditorUseFirstGraphicsAPIInfo, buildTargetParameter), buildTargetParameter);
            var requestCloseAndRelaunchWithCurrentArgumentsLambda = Expression.Lambda<Action>(Expression.Call(null, requestCloseAndRelaunchWithCurrentArgumentsInfo));
            GetLightmapEncodingQualityForPlatformGroup = getLightmapEncodingQualityForPlatformGroupLambda.Compile();
            SetLightmapEncodingQualityForPlatformGroup = setLightmapEncodingQualityForPlatformGroupLambda.Compile();
            CalculateSelectedBuildTarget = calculateSelectedBuildTargetLambda.Compile();
            GetSupportedGraphicsAPIs = getSupportedGraphicsAPIsLambda.Compile();
            WillEditorUseFirstGraphicsAPI = willEditorUseFirstGraphicsAPILambda.Compile();
            RequestCloseAndRelaunchWithCurrentArguments = requestCloseAndRelaunchWithCurrentArgumentsLambda.Compile();
        }

        #endregion

        #region HDRP_FIXES

        bool IsHDRPAllCorrect() =>
            IsLightmapCorrect()
            && IsShadowmaskCorrect()
            && IsColorSpaceCorrect()
            && IsHdrpAssetCorrect()
            && IsDefaultSceneCorrect();
        void FixHDRPAll()
            => EditorApplication.update += FixHDRPAllAsync;
        void FixHDRPAllAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being addressed
            if (!IsColorSpaceCorrect())
            {
                FixColorSpace();
                return;
            }
            if (!IsLightmapCorrect())
            {
                FixLightmap();
                return;
            }
            if (!IsShadowmaskCorrect())
            {
                FixShadowmask();
                return;
            }
            if (!IsHdrpAssetCorrect())
            {
                FixHdrpAssetAsync();
                return;
            }
            if (!IsDefaultSceneCorrect())
            {
                FixDefaultScene(async: true);
                return;
            }
            EditorApplication.update -= FixHDRPAllAsync;
        }

        bool IsHdrpAssetCorrect() =>
            IsHdrpAssetUsedCorrect()
            && IsHdrpAssetRuntimeResourcesCorrect()
            && IsHdrpAssetEditorResourcesCorrect()
            && IsHdrpAssetDiffusionProfileCorrect();
        void FixHdrpAsset() => EditorApplication.update += FixHdrpAssetAsync;

        void FixHdrpAssetAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being addressed
            if (!IsHdrpAssetUsedCorrect())
            {
                FixHdrpAssetUsed(async: true);
                return;
            }
            if (!IsHdrpAssetRuntimeResourcesCorrect())
            {
                FixHdrpAssetRuntimeResources();
                return;
            }
            if (!IsHdrpAssetEditorResourcesCorrect())
            {
                FixHdrpAssetEditorResources();
                return;
            }
            if (!IsHdrpAssetDiffusionProfileCorrect())
            {
                FixHdrpAssetDiffusionProfile();
            }
            EditorApplication.update -= FixHdrpAssetAsync;
        }


        bool IsColorSpaceCorrect()
            => PlayerSettings.colorSpace == ColorSpace.Linear;
        void FixColorSpace()
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
        void FixLightmap()
        {
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Standalone, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Lumin, LightmapEncodingQualityCopy.High);
            SetLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.WSA, LightmapEncodingQualityCopy.High);
        }

        bool IsShadowmaskCorrect()
            //QualitySettings.SetQualityLevel.set quality is too costy to be use at frame
            => QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask;
        void FixShadowmask()
        {
            int currentQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
            }
            QualitySettings.SetQualityLevel(currentQuality, applyExpensiveChanges: false);
        }

        bool IsHdrpAssetUsedCorrect()
            => GraphicsSettings.renderPipelineAsset != null && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset;
        void FixHdrpAssetUsed(bool async)
        {
            if (ObjectSelector.opened)
                return;
            CreateOrLoad<HDRenderPipelineAsset>(async
                ? () =>
                {
                    EditorApplication.update -= FixHdrpAssetAsync;
                //can also be called from fix all HDRP:
                EditorApplication.update -= FixHDRPAllAsync;
                }
            : (Action)null,
                asset => GraphicsSettings.renderPipelineAsset = asset);
        }

        bool IsHdrpAssetRuntimeResourcesCorrect()
            => IsHdrpAssetUsedCorrect()
            && HDRenderPipeline.defaultAsset.renderPipelineResources != null;
        void FixHdrpAssetRuntimeResources()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed(async: false);
            HDRenderPipeline.defaultAsset.renderPipelineResources
                = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset");
            ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsHdrpAssetEditorResourcesCorrect()
            => IsHdrpAssetUsedCorrect()
            && HDRenderPipeline.defaultAsset.renderPipelineEditorResources != null;
        void FixHdrpAssetEditorResources()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed(async: false);
            HDRenderPipeline.defaultAsset.renderPipelineEditorResources
                = AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset");
            ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineEditorResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsHdrpAssetDiffusionProfileCorrect()
        {
            var profileList = HDRenderPipeline.defaultAsset?.diffusionProfileSettingsList;
            return IsHdrpAssetUsedCorrect() && profileList.Length != 0 && profileList.Any(p => p != null);
        }

        void FixHdrpAssetDiffusionProfile()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed(async: false);

            var hdAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            hdAsset.diffusionProfileSettingsList = hdAsset.renderPipelineEditorResources.defaultDiffusionProfileSettingsList;
        }

        bool IsDefaultSceneCorrect()
            => HDProjectSettings.defaultScenePrefab != null;
        void FixDefaultScene(bool async)
        {
            if (ObjectSelector.opened)
                return;
            CreateOrLoadDefaultScene(async ? () => EditorApplication.update -= FixHDRPAllAsync : (Action)null, scene => HDProjectSettings.defaultScenePrefab = scene);
            m_DefaultScene.SetValueWithoutNotify(HDProjectSettings.defaultScenePrefab);
        }

        #endregion

        #region HDRP_VR_FIXES

        bool IsVRAllCorrect()
            => IsVRSupportedForCurrentBuildTargetGroupCorrect();
        void FixVRAll() => EditorApplication.update += FixVRAllAsync;
        void FixVRAllAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being addressed
            if (!IsVRSupportedForCurrentBuildTargetGroupCorrect())
            {
                FixVRSupportedForCurrentBuildTargetGroup();
                return;
            }
            EditorApplication.update -= FixVRAllAsync;
        }

        bool IsVRSupportedForCurrentBuildTargetGroupCorrect()
            => VREditor.GetVREnabledOnTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        void FixVRSupportedForCurrentBuildTargetGroup()
            => VREditor.SetVREnabledOnTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup, true);

        #endregion

        #region HDRP_DXR_FIXES

        bool IsDXRAllCorrect()
            => IsDXRAutoGraphicsAPICorrect()
            && IsDXRDirect3D12Correct()
            && IsDXRActivationCorrect()
            && IsDXRActivationCorrect()
            && IsDXRAssetCorrect();

        void FixDXRAll() => EditorApplication.update += FixDXRAllAsync;
        void FixDXRAllAsync()
        {
            //Async will allow to make things green when fixed before asking
            //new confirmation to fix elements. It help the user to know which
            //one is currently being addressed
            if (!IsDXRAutoGraphicsAPICorrect())
            {
                FixDXRAutoGraphicsAPI();
                return;
            }
            if (!IsDXRDirect3D12Correct())
            {
                FixDXRDirect3D12(fromAsync: true);
                return;
            }

            if (!IsScreenSpaceShadowCorrect())
            {
                FixScreenSpaceShadow();
                return;
            }
            if (!IsDXRActivationCorrect())
            {
                FixDXRActivation();
                return;
            }
            if (!IsDXRAssetCorrect())
            {
                FixDXRAsset();
                return;
            }
            EditorApplication.update -= FixDXRAllAsync;
        }

        bool IsDXRAutoGraphicsAPICorrect()
            => !PlayerSettings.GetUseDefaultGraphicsAPIs(CalculateSelectedBuildTarget());
        void FixDXRAutoGraphicsAPI()
            => PlayerSettings.SetUseDefaultGraphicsAPIs(CalculateSelectedBuildTarget(), false);

        bool IsDXRDirect3D12Correct()
            => PlayerSettings.GetGraphicsAPIs(CalculateSelectedBuildTarget()).FirstOrDefault() == GraphicsDeviceType.Direct3D12;
        void FixDXRDirect3D12(bool fromAsync)
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
                if (fromAsync)
                    EditorApplication.update -= FixDXRAllAsync;
                ChangedFirstGraphicAPI(buidTarget);
            }
        }

        void ChangedFirstGraphicAPI(BuildTarget target)
        {
            // If we're changing the first API for relevant editor, this will cause editor to switch: ask for scene save & confirmation
            if (WillEditorUseFirstGraphicsAPI(target))
            {
                if (EditorUtility.DisplayDialog("Changing editor graphics device",
                    "You've changed the active graphics API. This requires a restart of the Editor. After restarting finish fixing DXR configuration by launching the wizard again.",
                    "Restart Editor", "Not now"))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        RequestCloseAndRelaunchWithCurrentArguments();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        bool IsDXRAssetCorrect()
            => GraphicsSettings.renderPipelineAsset != null
            && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset
            && HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources != null;
        void FixDXRAsset()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed(async: false);
            HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources
                = AssetDatabase.LoadAssetAtPath<HDRenderPipelineRayTracingResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRayTracingResources.asset");
            ResourceReloader.ReloadAllNullIn(HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
        }

        bool IsScreenSpaceShadowCorrect()
            => GraphicsSettings.renderPipelineAsset != null
            && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset
            && (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows;
        void FixScreenSpaceShadow()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed(async: false);
            //as property returning struct make copy, use serializedproperty to modify it
            var serializedObject = new SerializedObject(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            var propertySupportScreenSpaceShadow = serializedObject.FindProperty("m_RenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows");
            propertySupportScreenSpaceShadow.boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        bool IsDXRActivationCorrect()
            => GraphicsSettings.renderPipelineAsset != null
            && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset
            && (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings.supportRayTracing;
        void FixDXRActivation()
        {
            if (!IsHdrpAssetUsedCorrect())
                FixHdrpAssetUsed(async: false);
            //as property returning struct make copy, use serializedproperty to modify it
            var serializedObject = new SerializedObject(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            var propertySupportRayTracing = serializedObject.FindProperty("m_RenderPipelineSettings.supportRayTracing");
            propertySupportRayTracing.boolValue = true;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}
