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
using static PerformanceMetricNames;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PerformanceTests : IPrebuildSetup
{
    protected static readonly int WarmupCount = 10;
    protected static readonly int MeasurementCount = 30;  // Number of frames to measure
    protected const int GlobalTimeout = 120 * 1000;       // 2 min
    protected const int BuildTimeout = 10 * 60 * 1000;    // 10 min for each build test
    protected const int minMemoryReportSize = 512 * 1024; // in bytes

    public void Setup()
    {
#if UNITY_EDITOR
        // Add all test scenes from the asset to the build settings:
        var testScenes = testScenesAsset.GetAllTests()
            .Select(test => {
            var scene = SceneManager.GetSceneByName(test.sceneData.scene);
            var sceneGUID = AssetDatabase.FindAssets($"t:Scene {test.sceneData.scene}").FirstOrDefault();
            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
            return new EditorBuildSettingsScene(scenePath, true);
        });

        EditorBuildSettings.scenes = testScenes.ToArray();
#endif
    }

    public struct CounterTestDescription
    {
        public TestSceneAsset.SceneData    sceneData;
        public TestSceneAsset.HDAssetData  assetData;

        public override string ToString()
            => PerformanceTestUtils.FormatTestName(sceneData.scene, sceneData.sceneLabels, String.IsNullOrEmpty(assetData.alias) ? assetData.asset.name : assetData.alias, assetData.assetLabels, kDefault);
    }

    public static IEnumerable<CounterTestDescription> GetCounterTests()
    {
        foreach (var (scene, asset) in testScenesAsset.counterTestSuite.GetTestList())
            yield return new CounterTestDescription{ assetData = asset, sceneData = scene };
    }

    IEnumerable<HDProfileId> GetAllMarkers()
    {
        foreach (var val in Enum.GetValues(typeof(HDProfileId)))
            yield return (HDProfileId)val;
    }

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Counters([ValueSource(nameof(GetCounterTests))] CounterTestDescription testDescription)
    {
        yield return SetupTest(testDescription.sceneData.scene, testDescription.assetData.asset);

        var camera = GameObject.FindObjectOfType<Camera>();
        var hdCamera = HDCamera.GetOrCreate(camera, 0); // We don't support XR for now

        // Enable all the markers
        hdCamera.profilingSampler.enableRecording = true;
        foreach (var marker in GetAllMarkers())
            ProfilingSampler.Get(marker).enableRecording = true;

        // Wait for the markers to be initialized
        for (int i = 0; i < 20; i++)
            yield return null;

        for (int i = 0; i < MeasurementCount; i++)
        {
            MeasureTime(hdCamera.profilingSampler);
            foreach (var marker in GetAllMarkers())
                MeasureTime(ProfilingSampler.Get(marker));
        }

        // disable all the markers
        hdCamera.profilingSampler.enableRecording = false;
        foreach (var marker in GetAllMarkers())
            ProfilingSampler.Get(marker).enableRecording = false;

        void MeasureTime(ProfilingSampler sampler)
        {
            // Due to a bug about convertion of time units before sending the data to the database, we need to use Undefined units
            SampleGroup cpuSample = new SampleGroup(FormatSampleGroupName(kTiming, kCPU, sampler.name), SampleUnit.Undefined, false);
            SampleGroup gpuSample = new SampleGroup(FormatSampleGroupName(kTiming, kGPU, sampler.name), SampleUnit.Undefined, false);
            SampleGroup inlineCPUSample = new SampleGroup(FormatSampleGroupName(kTiming, kInlineCPU, sampler.name), SampleUnit.Undefined, false);

            if (sampler.cpuElapsedTime > 0)
                Measure.Custom(cpuSample, sampler.cpuElapsedTime);
            if (sampler.gpuElapsedTime > 0)
                Measure.Custom(gpuSample, sampler.gpuElapsedTime);
            if (sampler.inlineCpuElapsedTime > 0)
                Measure.Custom(inlineCPUSample, sampler.inlineCpuElapsedTime);
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

    public static IEnumerable<MemoryTestDescription> GetMemoryTests()
    {
        foreach (var (scene, asset) in testScenesAsset.memoryTestSuite.GetTestList())
            foreach (var objectType in GetMemoryObjectTypes())
                yield return new MemoryTestDescription{ assetData = asset, sceneData = scene, assetType = objectType };
    }

    public struct MemoryTestDescription
    {
        public TestSceneAsset.SceneData     sceneData;
        public TestSceneAsset.HDAssetData   assetData;
        public Type                         assetType;

        public override string ToString()
            => PerformanceTestUtils.FormatTestName(sceneData.scene, sceneData.sceneLabels, String.IsNullOrEmpty(assetData.alias) ? assetData.asset.name : assetData.alias, assetData.assetLabels, assetType.Name);
    }

    [Timeout(BuildTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Memory([ValueSource(nameof(GetMemoryTests))] MemoryTestDescription testDescription)
    {
        yield return SetupTest(testDescription.sceneData.scene, testDescription.assetData.asset);

        long totalMemory = 0;
        var data = Resources.FindObjectsOfTypeAll(testDescription.assetType);
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
            Measure.Custom(new SampleGroup(FormatSampleGroupName(kMemory, result.name), SampleUnit.Undefined, false), result.size);
        Measure.Custom(new SampleGroup(FormatSampleGroupName(kTotalMemory, testDescription.assetType.Name), SampleUnit.Undefined, false), totalMemory);
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