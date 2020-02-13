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

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HDRP_PerformaceTests : IPrebuildSetup, IPostBuildCleanup
{
    protected static readonly int WarmupCount = 10;
    protected static readonly int MeasurementCount = 100;
    protected const int GlobalTimeout = 120 * 1000;
    const int minMemoryReportSize = 512; // in bytes

    public const string testSceneResourcePath = "TestScenes";

    static TestSceneAsset testScenesAsset = Resources.Load<TestSceneAsset>(testSceneResourcePath);
    static HDRenderPipelineAsset defaultHDAsset = Resources.Load<HDRenderPipelineAsset>("defaultHDAsset");

    public enum Config
    {
        Forward,
        Deferred,
    }

    public static readonly Config[] configs =
    {
        Config.Forward,
        Config.Deferred,
    };

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

        Debug.Log(testScenes.Count());

        EditorBuildSettings.scenes = testScenes.ToArray();
#endif
    }

    public void Cleanup()
    {
    }

    static IEnumerable<string> EnumerateTestScenes(IEnumerable<TestSceneAsset.SceneData> sceneDatas)
    {
        foreach (var sceneData in sceneDatas)
            if (sceneData.enabled)
                yield return sceneData.scene;
    }

    public static IEnumerable<string> GetScenesForCounters() => EnumerateTestScenes(testScenesAsset.performanceCounterScenes);
    public static IEnumerable<string> GetScenesForMemory() => EnumerateTestScenes(testScenesAsset.memoryTestScenes);
    public static IEnumerable<string> GetScenesForBuild() => EnumerateTestScenes(testScenesAsset.buildTestScenes);
    public static IEnumerable<HDRenderPipelineAsset> GetHDAssetsForCounters() => testScenesAsset.performanceCounterHDAssets;
    public static IEnumerable<HDRenderPipelineAsset> GetHDAssetsForMemory() => testScenesAsset.memoryTestHDAssets;
    public static IEnumerable<HDRenderPipelineAsset> GetHDAssetsForBuild() => testScenesAsset.buildHDAssets;

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Counters(
        [ValueSource("GetScenesForCounters")] string sceneName,
        [ValueSource("GetHDAssetsForCounters")] HDRenderPipelineAsset hdAsset)
    {
        yield return SetupTest(sceneName, hdAsset);

        var camera = GameObject.FindObjectOfType<Camera>();
        var hdCamera = HDCamera.GetOrCreate(camera, 0); // We don't support XR for now

        SampleGroup cameraGPU = new SampleGroup("GPU Camera", SampleUnit.Millisecond, false);
        SampleGroup cameraCPU = new SampleGroup("CPU Camera", SampleUnit.Millisecond, false);
        SampleGroup gBufferGPU = new SampleGroup("GPU GBuffer", SampleUnit.Millisecond, false);
        SampleGroup gBufferCPU = new SampleGroup("CPU GBuffer", SampleUnit.Millisecond, false);
        SampleGroup sampleCount = new SampleGroup("sampleCount", SampleUnit.Second, false);
        var g =  ProfilingSampler.Get(HDProfileId.GBuffer);

        hdCamera.profilingSampler.enableRecording = true;
        g.enableRecording = true;

        for (int i = 0; i < 100; i++)
            yield return null;

        for (int i = 0; i < MeasurementCount; ++i)
        {
            using (Measure.Scope("GPU Counters"))
            {
                Measure.Custom(gBufferGPU, g.gpuElapsedTime);
                Measure.Custom(cameraGPU, hdCamera.profilingSampler.gpuElapsedTime);
            }
            using (Measure.Scope("CPU Counters"))
            {
                Measure.Custom(gBufferCPU, g.cpuElapsedTime);
                Measure.Custom(cameraCPU, hdCamera.profilingSampler.cpuElapsedTime);
            }
            yield return null;
        }

        hdCamera.profilingSampler.enableRecording = false;
        g.enableRecording = false;
    }

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Memory(
        [ValueSource("GetScenesForMemory")] string sceneName,
        [ValueSource("GetHDAssetsForMemory")] HDRenderPipelineAsset hdAsset)
    {
        yield return SetupTest(sceneName, hdAsset);

        using (Measure.Scope("Memory"))
        {
            MeasureMemory(typeof(RenderTexture));
            MeasureMemory(typeof(Texture2D));
            MeasureMemory(typeof(Texture3D));
            MeasureMemory(typeof(CubemapArray));
            MeasureMemory(typeof(Material));
            MeasureMemory(typeof(Mesh));
            MeasureMemory(typeof(Shader));
            MeasureMemory(typeof(ComputeShader));
        }
    }

    void MeasureMemory(System.Type type)
    {
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
        using (Measure.Scope($"Memory - {type}"))
        {
            foreach (var result in results)
                Measure.Custom(new SampleGroup(result.name, SampleUnit.Byte, false), result.size);
        }
        Measure.Custom(new SampleGroup("Total Memory - {type}", SampleUnit.Byte, false), totalMemory);
    }

    static IEnumerator SetupTest(string sceneName, HDRenderPipelineAsset hdAsset)
    {
        hdAsset = hdAsset ?? defaultHDAsset;
        if (GraphicsSettings.renderPipelineAsset != hdAsset)
            GraphicsSettings.renderPipelineAsset = hdAsset;

        SceneManager.LoadScene(sceneName);

        // Wait one frame for the scene to finish loading:
        yield return null;
    }
}