using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Profiling;
using static PerformanceTestUtils;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PerformanceTests : IPrebuildSetup
{
    protected static readonly int WarmupCount = 10;
    protected static readonly int MeasurementCount = 100;
    protected const int GlobalTimeout = 120 * 1000; // 2 min
    protected const int BuildTimeout = 10 * 60 * 1000; // 10 min for each build test
    const int minMemoryReportSize = 512 * 1024; // in bytes

    public void Setup()
    {
#if UNITY_EDITOR
        // Add all test scenes from the asset to the build settings:
        var testScenes = EnumerateTestScenes(testScenesAsset.GetAllScenes())
            .Select(sceneName => {
            var scene = SceneManager.GetSceneByName(sceneName);
            var sceneGUID = AssetDatabase.FindAssets($"t:Scene {sceneName}").FirstOrDefault();
            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
            return new EditorBuildSettingsScene("Assets/PerformanceTests/Scenes/0000_LitCube.unity", true);
        });

        EditorBuildSettings.scenes = testScenes.ToArray();
#endif
    }

    public static IEnumerable<string> GetScenesForCounters() => EnumerateTestScenes(testScenesAsset.performanceCounterScenes);
    public static IEnumerable<string> GetScenesForMemory() => EnumerateTestScenes(testScenesAsset.memoryTestScenes);
    public static IEnumerable<HDRenderPipelineAsset> GetHDAssetsForCounters() => testScenesAsset.performanceCounterHDAssets;
    public static IEnumerable<HDRenderPipelineAsset> GetHDAssetsForMemory() => testScenesAsset.memoryTestHDAssets;

    HDProfileId[] profiledMarkers = new HDProfileId[] {
        HDProfileId.VolumeUpdate,
        HDProfileId.ClearBuffers,
        HDProfileId.RenderShadowMaps,
        HDProfileId.GBuffer,
        HDProfileId.PrepareLightsForGPU,
        HDProfileId.VolumeVoxelization,
        HDProfileId.VolumetricLighting,
        HDProfileId.RenderDeferredLightingCompute,
        HDProfileId.ForwardOpaque,
        HDProfileId.ForwardTransparent,
        HDProfileId.ForwardPreRefraction,
        HDProfileId.ColorPyramid,
        HDProfileId.DepthPyramid,
        HDProfileId.PostProcessing,
    };

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Counters(
        [ValueSource(nameof(GetScenesForCounters))] string sceneName,
        [ValueSource(nameof(GetHDAssetsForCounters))] HDRenderPipelineAsset hdAsset)
    {
        yield return SetupTest(sceneName, hdAsset);

        var camera = GameObject.FindObjectOfType<Camera>();
        var hdCamera = HDCamera.GetOrCreate(camera, 0); // We don't support XR for now

        // Enable all the markers
        hdCamera.profilingSampler.enableRecording = true;
        foreach (var marker in profiledMarkers)
            ProfilingSampler.Get(marker).enableRecording = true;

        // Wait for the markers to be initialized
        for (int i = 0; i < 20; i++)
            yield return null;

        for (int i = 0; i < MeasurementCount; i++)
        {
            MeasureTime(hdCamera.profilingSampler);
            foreach (var marker in profiledMarkers)
                MeasureTime(ProfilingSampler.Get(marker));
        }

        // disable all the markers
        hdCamera.profilingSampler.enableRecording = false;
        foreach (var marker in profiledMarkers)
            ProfilingSampler.Get(marker).enableRecording = false;
        
        void MeasureTime(ProfilingSampler sampler)
        {
            SampleGroup cpuSample = new SampleGroup($"CPU {sampler.name}", SampleUnit.Millisecond, false);
            SampleGroup gpuSample = new SampleGroup($"GPU {sampler.name}", SampleUnit.Millisecond, false);

            Measure.Custom(cpuSample, sampler.cpuElapsedTime);
            Measure.Custom(gpuSample, sampler.gpuElapsedTime);
        }
    }

    static IEnumerable<Type> GetMemoryObjectTypes()
    {
        yield return typeof(RenderTexture);
        yield return typeof(Texture2D);
        yield return typeof(Texture3D);
        yield return typeof(CubemapArray);
        yield return typeof(Material);
        yield return typeof(Mesh);
        yield return typeof(Shader);
        yield return typeof(ComputeShader);
    }

    [Timeout(BuildTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Memory(
        [ValueSource(nameof(GetScenesForMemory))] string sceneName,
        [ValueSource(nameof(GetMemoryObjectTypes))] Type type,
        [ValueSource(nameof(GetHDAssetsForMemory))] HDRenderPipelineAsset hdAsset)
    {
        yield return SetupTest(sceneName, hdAsset);

        long totalMemory = 0;
        var data = Resources.FindObjectsOfTypeAll(type);
        var results = new List<(string name, long size)>();

        // Measure memory
        foreach (var item in data)
        {
            string name = String.IsNullOrEmpty(item.name) ? item.GetType().Name : item.name;
            long currSize = Profiler.GetRuntimeMemorySizeLong(item);

            // There are too many items here so we only keep the one that have a minimun of weight
            if (currSize > minMemoryReportSize)
            {
                results.Add((name, currSize));
                totalMemory += currSize;
            }
        }

        results.Sort((a, b) => b.size.CompareTo(a.size));

        // Report data
        foreach (var result in results)
            Measure.Custom(new SampleGroup(result.name, SampleUnit.Byte, false), result.size);
        Measure.Custom(new SampleGroup($"Total Memory - {type}", SampleUnit.Byte, false), totalMemory);
    }

    [Version("1"), UnityTest]
    public bool _DumpAllSystemInfo()
    {
        // Display all stats that will be available in the performance database:
        Debug.Log($"PlayerSystemInfo.OperatingSystem: {SystemInfo.operatingSystem}");
        Debug.Log($"PlayerSystemInfo.DeviceModel: {SystemInfo.deviceModel}");
        Debug.Log($"PlayerSystemInfo.DeviceName: {SystemInfo.deviceName}");
        Debug.Log($"PlayerSystemInfo.ProcessorType: {SystemInfo.processorType}");
        Debug.Log($"PlayerSystemInfo.ProcessorCount: {SystemInfo.processorCount}");
        Debug.Log($"PlayerSystemInfo.GraphicsDeviceName: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"PlayerSystemInfo.SystemMemorySize: {SystemInfo.systemMemorySize}");

        Debug.Log($"QualitySettings.Vsync: {QualitySettings.vSyncCount}");
        Debug.Log($"QualitySettings.AntiAliasing: {QualitySettings.antiAliasing}");
        // Debug.Log($"QualitySettings.ColorSpace: {QualitySettings.activeColorSpace.ToString()}");
        Debug.Log($"QualitySettings.AnisotropicFiltering: {QualitySettings.anisotropicFiltering.ToString()}");
        Debug.Log($"QualitySettings.BlendWeights: {QualitySettings.skinWeights.ToString()}");
        Debug.Log($"ScreenSettings.ScreenRefreshRate: {Screen.currentResolution.refreshRate}");
        Debug.Log($"ScreenSettings.ScreenWidth: {Screen.currentResolution.width}");
        Debug.Log($"ScreenSettings.ScreenHeight: {Screen.currentResolution.height}");
        Debug.Log($"ScreenSettings.Fullscreen: {Screen.fullScreen}");
        // Debug.Log($"{(Application.isEditor ? true : Debug.isDebugBuild)}");
        Debug.Log($"BuildSettings. Platform: {Application.platform.ToString()}");

        return true;
    }
}