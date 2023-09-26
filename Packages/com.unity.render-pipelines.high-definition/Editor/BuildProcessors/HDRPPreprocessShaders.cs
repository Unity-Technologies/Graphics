using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Rendering.HighDefinition
{
    // The common shader stripper function
    class CommonShaderPreprocessor : BaseShaderPreprocessor
    {
        public override int Priority => 100;

        public CommonShaderPreprocessor() { }

        protected override bool DoShadersStripper(HDRenderPipelineAsset hdrpAsset, Shader shader, ShaderSnippetData snippet, ShaderCompilerData inputData)
        {
            // CAUTION: Pass Name and Lightmode name must match in master node and .shader.
            // HDRP use LightMode to do drawRenderer and pass name is use here for stripping!

            var globalSettings = HDRenderPipelineGlobalSettings.Ensure();

            // Remove water if disabled
            if (!hdrpAsset.currentPlatformRenderPipelineSettings.supportWater)
            {
                if (shader == hdrpAsset.renderPipelineResources.shaders.waterCausticsPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.waterPS)
                    return true;
            }
            
            // If Data Driven Lens Flare is disabled, strip all the shaders (the preview shader LensFlareDataDrivenPreview.shader in Core will not be stripped)
            if (!hdrpAsset.currentPlatformRenderPipelineSettings.supportDataDrivenLensFlare)
            {
                if (shader == hdrpAsset.renderPipelineResources.shaders.lensFlareDataDrivenPS)
                    return true;
            }

            // Remove editor only pass
            bool isSceneSelectionPass = snippet.passName == "SceneSelectionPass";
            bool isScenePickingPass = snippet.passName == "ScenePickingPass";
            bool metaPassUnused = (snippet.passName == "META") && (SupportedRenderingFeatures.active.enlighten == false ||
                ((int)SupportedRenderingFeatures.active.lightmapBakeTypes | (int)LightmapBakeType.Realtime) == 0);
            bool editorVisualization = inputData.shaderKeywordSet.IsEnabled(m_EditorVisualization);
            if (isSceneSelectionPass || isScenePickingPass || metaPassUnused || editorVisualization)
                return true;

            // CAUTION: We can't identify transparent material in the stripped in a general way.
            // Shader Graph don't produce any keyword - However it will only generate the pass that are required, so it already handle transparent (Note that shader Graph still define _SURFACE_TYPE_TRANSPARENT but as a #define)
            // For inspector version of shader, we identify transparent with a shader feature _SURFACE_TYPE_TRANSPARENT.
            // Only our Lit (and inherited) shader use _SURFACE_TYPE_TRANSPARENT, so the specific stripping based on this keyword is in LitShadePreprocessor.
            // Here we can't strip based on opaque or transparent but we will strip based on HDRP Asset configuration.

            bool isMotionPass = snippet.passName == "MotionVectors";
            if (isMotionPass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                return true;

            bool isDistortionPass = snippet.passName == "DistortionVectors";
            if (isDistortionPass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportDistortion)
                return true;

            bool isTransparentBackface = snippet.passName == "TransparentBackface";
            if (isTransparentBackface && !hdrpAsset.currentPlatformRenderPipelineSettings.supportTransparentBackface)
                return true;

            bool isTransparentPrepass = snippet.passName == "TransparentDepthPrepass";
            if (isTransparentPrepass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportTransparentDepthPrepass)
                return true;

            bool isTransparentPostpass = snippet.passName == "TransparentDepthPostpass";
            if (isTransparentPostpass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportTransparentDepthPostpass)
                return true;

            bool isRayTracingPrepass = snippet.passName == "RayTracingPrepass";
            if (isRayTracingPrepass && !hdrpAsset.currentPlatformRenderPipelineSettings.supportRayTracing)
                return true;

            // If requested by the render pipeline settings, or if we are in a release build,
            // don't compile fullscreen debug display variant
            bool isFullScreenDebugPass = snippet.passName == "FullScreenDebug";
            if (isFullScreenDebugPass && (!Debug.isDebugBuild || !globalSettings.supportRuntimeDebugDisplay))
                return true;

            // Debug Display shader is currently the longest shader to compile, so we allow users to disable it at runtime.
            // We also don't want it in release build.
            // However our AOV API rely on several debug display shader. In case AOV API is requested at runtime (like for the Graphics Compositor)
            // we allow user to make explicit request for it and it bypass other request
            if ((!Debug.isDebugBuild || !globalSettings.supportRuntimeDebugDisplay) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportRuntimeAOVAPI)
            {
                if (shader == hdrpAsset.renderPipelineResources.shaders.debugDisplayLatlongPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugViewMaterialGBufferPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugViewTilesPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugFullScreenPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugColorPickerPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugExposurePS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugHDRPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugLightVolumePS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugBlitQuad ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugViewVirtualTexturingBlit ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugWaveformPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugVectorscopePS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.debugLocalVolumetricFogAtlasPS)
                    return true;

                if (inputData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                    return true;
            }

            // Remove APV debug if disabled
            if ((!Debug.isDebugBuild || !globalSettings.supportRuntimeDebugDisplay) || !hdrpAsset.currentPlatformRenderPipelineSettings.supportProbeVolume)
            {
                if (shader == hdrpAsset.renderPipelineResources.shaders.probeVolumeDebugShader ||
                    shader == hdrpAsset.renderPipelineResources.shaders.probeVolumeOffsetDebugShader)
                    return true;
            }

            if (inputData.shaderKeywordSet.IsEnabled(m_LodFadeCrossFade) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportDitheringCrossFade)
                return true;

            if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly)
            {
                if (shader == hdrpAsset.renderPipelineResources.shaders.deferredPS ||
                    shader == hdrpAsset.renderPipelineResources.shaders.deferredTilePS)
                    return true;
            }

            if (inputData.shaderKeywordSet.IsEnabled(m_WriteMSAADepth) && (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly))
                return true;

            if (!hdrpAsset.currentPlatformRenderPipelineSettings.supportSubsurfaceScattering)
            {
                if (shader == hdrpAsset.renderPipelineResources.shaders.combineLightingPS)
                    return true;
                // Note that this is only going to affect the deferred shader and for a debug case, so it won't save much.
                if (inputData.shaderKeywordSet.IsEnabled(m_SubsurfaceScattering))
                    return true;
            }

            if (!hdrpAsset.currentPlatformRenderPipelineSettings.lightLoopSettings.supportFabricConvolution)
            {
                if (shader == hdrpAsset.renderPipelineResources.shaders.charlieConvolvePS)
                    return true;
            }

            if (inputData.shaderKeywordSet.IsEnabled(m_Transparent))
            {
                // If transparent we don't need the depth only pass
                bool isDepthOnlyPass = snippet.passName == "DepthForwardOnly";
                if (isDepthOnlyPass)
                    return true;

                // If transparent we don't need the motion vector pass
                if (isMotionPass)
                    return true;

                // If we are transparent we use cluster lighting and not tile lighting
                if (inputData.shaderKeywordSet.IsEnabled(m_TileLighting))
                    return true;
            }
            else // Opaque
            {
                // If opaque, we never need transparent specific passes (even in forward only mode)
                bool isTransparentForwardPass = isTransparentPostpass || isTransparentBackface || isTransparentPrepass || isDistortionPass;
                if (isTransparentForwardPass)
                    return true;

                // TODO: Should we remove Cluster version if we know MSAA is disabled ? This prevent to manipulate LightLoop Settings (useFPTL option)
                // For now comment following code
                // if (inputData.shaderKeywordSet.IsEnabled(m_ClusterLighting) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportMSAA)
                //    return true;
            }

            // SHADOW

            // Strip every useless shadow configs
            var shadowInitParams = hdrpAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams;

            foreach (var shadowVariant in m_ShadowKeywords.ShadowVariants)
            {
                if (shadowVariant.Key != shadowInitParams.shadowFilteringQuality)
                    if (inputData.shaderKeywordSet.IsEnabled(shadowVariant.Value))
                        return true;
            }

            foreach (var areaShadowVariant in m_ShadowKeywords.AreaShadowVariants)
            {
                if (areaShadowVariant.Key != shadowInitParams.areaShadowFilteringQuality)
                    if (inputData.shaderKeywordSet.IsEnabled(areaShadowVariant.Value))
                        return true;
            }

            if (!shadowInitParams.supportScreenSpaceShadows && shader == hdrpAsset.renderPipelineResources.shaders.screenSpaceShadowPS)
                return true;

            // Screen space shadow variant is exclusive, either we have a variant with dynamic if that support screen space shadow or not
            // either we have a variant that don't support at all. We can't have both at the same time.
            if (inputData.shaderKeywordSet.IsEnabled(m_ScreenSpaceShadowOFFKeywords) && shadowInitParams.supportScreenSpaceShadows)
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_ScreenSpaceShadowONKeywords) && !shadowInitParams.supportScreenSpaceShadows)
                return true;

            // DECAL

            // Strip the decal prepass variant when decals are disabled
            if (inputData.shaderKeywordSet.IsEnabled(m_WriteDecalBuffer) &&
                !(hdrpAsset.currentPlatformRenderPipelineSettings.supportDecals && hdrpAsset.currentPlatformRenderPipelineSettings.supportDecalLayers))
                return true;

            // If decal support, remove unused variant
            if (hdrpAsset.currentPlatformRenderPipelineSettings.supportDecals)
            {
                // Remove the no decal case
                if (inputData.shaderKeywordSet.IsEnabled(m_DecalsOFF))
                    return true;

                // If decal but with 4RT remove 3RT variant and vice versa for both Material and Decal Material
                if (inputData.shaderKeywordSet.IsEnabled(m_Decals3RT) && hdrpAsset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask)
                    return true;

                if (inputData.shaderKeywordSet.IsEnabled(m_Decals4RT) && !hdrpAsset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask)
                    return true;

                // Remove the surface gradient blending if not enabled
                if (inputData.shaderKeywordSet.IsEnabled(m_DecalSurfaceGradient) && !hdrpAsset.currentPlatformRenderPipelineSettings.supportSurfaceGradient)
                    return true;
            }
            else
            {
                // Strip if it is a decal pass
                bool isDBufferMesh = snippet.passName == "DBufferMesh";
                bool isDecalMeshForwardEmissive = snippet.passName == "DecalMeshForwardEmissive";
                bool isDBufferProjector = snippet.passName == "DBufferProjector";
                bool isDecalProjectorForwardEmissive = snippet.passName == "DecalProjectorForwardEmissive";
                if (isDBufferMesh || isDecalMeshForwardEmissive || isDBufferProjector || isDecalProjectorForwardEmissive)
                    return true;

                // If no decal support, remove decal variant
                if (inputData.shaderKeywordSet.IsEnabled(m_Decals3RT) || inputData.shaderKeywordSet.IsEnabled(m_Decals4RT))
                    return true;

                // Remove the surface gradient blending
                if (inputData.shaderKeywordSet.IsEnabled(m_DecalSurfaceGradient))
                    return true;
            }

            // Global Illumination
            if (inputData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL1) &&
                (!hdrpAsset.currentPlatformRenderPipelineSettings.supportProbeVolume || hdrpAsset.currentPlatformRenderPipelineSettings.probeVolumeSHBands != ProbeVolumeSHBands.SphericalHarmonicsL1))
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL2) &&
                (!hdrpAsset.currentPlatformRenderPipelineSettings.supportProbeVolume || hdrpAsset.currentPlatformRenderPipelineSettings.probeVolumeSHBands != ProbeVolumeSHBands.SphericalHarmonicsL2))
                return true;

#if !ENABLE_SENSOR_SDK
            // If the SensorSDK package is not present, make sure that all code related to it is stripped away
            if (inputData.shaderKeywordSet.IsEnabled(m_SensorEnableLidar) || inputData.shaderKeywordSet.IsEnabled(m_SensorOverrideReflectance))
                return true;
#endif

            // HDR Output
            if (!HDROutputUtils.IsShaderVariantValid(inputData.shaderKeywordSet, PlayerSettings.allowHDRDisplaySupport))
                 return true;

            return false;
        }
    }

    // Build preprocessor to find all potentially used HDRP assets.
    class ShaderBuildPreprocessor : IPreprocessBuildWithReport
    {
        private static List<HDRenderPipelineAsset> _hdrpAssets;
        private static Dictionary<int, ComputeShader> s_ComputeShaderCache;
        private static bool s_PlayerNeedRaytracing;

        public static List<HDRenderPipelineAsset> hdrpAssets
        {
            get
            {
                if (_hdrpAssets == null || _hdrpAssets.Count == 0)
                    GetAllValidHDRPAssets(EditorUserBuildSettings.activeBuildTarget);
                return _hdrpAssets;
            }
        }


        public static Dictionary<int, ComputeShader> computeShaderCache
        {
            get
            {
                if (s_ComputeShaderCache == null)
                    BuilRaytracingComputeList();
                return s_ComputeShaderCache;
            }
        }

        public static bool playerNeedRaytracing
        {
            get
            {
                return s_PlayerNeedRaytracing;
            }
        }

        public static void BuilRaytracingComputeList()
        {
            if (s_ComputeShaderCache != null)
                s_ComputeShaderCache.Clear();
            else
                s_ComputeShaderCache = new Dictionary<int, ComputeShader>();

            if (HDRenderPipelineGlobalSettings.Ensure(canCreateNewAsset: false) == null)
                return;

            if (HDRenderPipelineGlobalSettings.instance.renderPipelineRayTracingResources == null)
                return;

            foreach (var fieldInfo in HDRenderPipelineGlobalSettings.instance.renderPipelineRayTracingResources.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                ComputeShader computeshader;
                computeshader = fieldInfo.GetValue(HDRenderPipelineGlobalSettings.instance.renderPipelineRayTracingResources) as ComputeShader;

                if (computeshader != null)
                {
                    s_ComputeShaderCache.Add(computeshader.GetInstanceID(), computeshader);
                }
            }
        }

        static void GetAllValidHDRPAssets(BuildTarget buildTarget)
        {
            s_PlayerNeedRaytracing = false;

            if (HDRenderPipeline.currentAsset == null)
                return;

            if (_hdrpAssets != null)
                _hdrpAssets.Clear();
            else
                _hdrpAssets = new List<HDRenderPipelineAsset>();

            // Here we want the HDRP Assets that are actually used at runtime.
            // An SRP asset is included if:
            // 1. It is set in an enabled quality level
            // 2. It is set as main (GraphicsSettings.renderPipelineAsset)
            //   AND at least one quality level does not have SRP override
            // Fetch all SRP overrides in all enabled quality levels for this platform
            buildTarget.TryGetRenderPipelineAssets<HDRenderPipelineAsset>(_hdrpAssets);

            // Get all enabled scenes path in the build settings.
            var scenesPaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path);

            // Find all HDRP assets that are dependencies of the scenes.
            var depsArray = AssetDatabase.GetDependencies(scenesPaths.ToArray());
            HashSet<string> depsHash = new HashSet<string>(depsArray);

            var guidRenderPipelineAssets = AssetDatabase.FindAssets("t:HDRenderPipelineAsset");

            for (int i = 0; i < guidRenderPipelineAssets.Length; ++i)
            {
                var curGUID = guidRenderPipelineAssets[i];
                var curPath = AssetDatabase.GUIDToAssetPath(curGUID);
                if (depsHash.Contains(curPath))
                {
                    _hdrpAssets.Add(AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(curPath));
                }
            }

            // Add the HDRP assets that are in the Resources folders.
            // Do not call this function below because it cause the issue https://fogbugz.unity3d.com/f/cases/1417508/
            // Users will need to rely on label instead.
            //_hdrpAssets.AddRange(Resources.LoadAll<HDRenderPipelineAsset>(""));

            // Add the HDRP assets that are labeled to be included
            _hdrpAssets.AddRange(
                AssetDatabase.FindAssets("t:HDRenderPipelineAsset l:" + HDEditorUtils.HDRPAssetBuildLabel)
                    .Select(s => AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(s)))
            );

            // Discard duplicate entries
            using (HashSetPool<HDRenderPipelineAsset>.Get(out var uniques))
            {
                foreach (var hdrpAsset in _hdrpAssets)
                    uniques.Add(hdrpAsset);
                _hdrpAssets.Clear();
                _hdrpAssets.AddRange(uniques);
            }

            // Prompt a warning if we find 0 HDRP Assets.
            if (_hdrpAssets.Count == 0)
            {
                if (!Application.isBatchMode)
                {
                    if (!EditorUtility.DisplayDialog("HDRP Asset missing", "No HDRP Asset has been set in the Graphic Settings, and no potential used in the build HDRP Asset has been found. If you want to continue compiling, this might lead to VERY long compilation time.", "Ok", "Cancel"))
                        throw new UnityEditor.Build.BuildFailedException("Build canceled");
                }
                else
                {
                    Debug.LogWarning("There is no HDRP Asset provided in GraphicsSettings. Build time can be extremely long without it.");
                }
            }
            else
            {
                // Take the opportunity to know if we need raytracing at runtime
                foreach (var hdrpAsset in _hdrpAssets)
                {
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportRayTracing)
                        s_PlayerNeedRaytracing = true;
                }
            }

            Debug.Log(string.Format("{0} HDRP assets included in build:{1}",
                _hdrpAssets.Count,
                _hdrpAssets
                    .Select(a => a.name)
                    .Aggregate("", (current, next) => $"{current}{System.Environment.NewLine}- {next}")
                ));

        }

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            GetAllValidHDRPAssets(EditorUserBuildSettings.activeBuildTarget);
        }
    }
}
