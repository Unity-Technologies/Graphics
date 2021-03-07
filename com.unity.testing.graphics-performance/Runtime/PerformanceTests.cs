using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;
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
    public void Setup()
    {
#if UNITY_EDITOR
        // Add all test scenes from the asset to the build settings:
        if (testScenesAsset)
        {
            var testScenes = testScenesAsset.GetAllTests()
                                            .Select(test =>
                                            {
                                                var scene = SceneManager.GetSceneByName(test.sceneData.scene);
                                                var sceneGUID = AssetDatabase.FindAssets($"t:Scene {test.sceneData.scene}").FirstOrDefault();
                                                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
                                                return new EditorBuildSettingsScene(scenePath, true);
                                            });
            EditorBuildSettings.scenes = testScenes.ToArray();
        }

        EditorUserBuildSettings.ps4HardwareTarget = PS4HardwareTarget.BaseOnly;
#endif
    }

    // Auto cleanup when the test exits
    [TearDown]
    public void TearDown() => CleanupTestSceneIfNeeded();

    protected IEnumerator MeasureProfilingSamplers(IEnumerable<ProfilingSampler> samplers, int warmupFramesCount = 20, int measureFrameCount = 30)
    {
        // Enable all the markers
        foreach (var sampler in samplers)
            sampler.enableRecording = true;

        // Allocate all sample groups:
        var sampleGroups = new Dictionary<ProfilingSampler, (SampleGroup cpu, SampleGroup inlineCPU, SampleGroup gpu)>();
        foreach (var sampler in samplers)
            CreateSampleGroups(sampler);

        // Wait for the markers to be initialized
        for (int i = 0; i < warmupFramesCount; i++)
            yield return null;

        for (int i = 0; i < measureFrameCount; i++)
        {
            foreach (var sampler in samplers)
                MeasureTime(sampler);
            yield return null;
        }

        // disable all the markers
        foreach (var sampler in samplers)
            sampler.enableRecording = false;

        void CreateSampleGroups(ProfilingSampler sampler)
        {
            SampleGroup cpuSample = new SampleGroup(FormatSampleGroupName(k_Timing, k_CPU, sampler.name), SampleUnit.Millisecond, false);
            SampleGroup gpuSample = new SampleGroup(FormatSampleGroupName(k_Timing, k_GPU, sampler.name), SampleUnit.Millisecond, false);
            SampleGroup inlineCPUSample = new SampleGroup(FormatSampleGroupName(k_Timing, k_InlineCPU, sampler.name), SampleUnit.Millisecond, false);
            sampleGroups[sampler] = (cpuSample, inlineCPUSample, gpuSample);
        }

        void MeasureTime(ProfilingSampler sampler)
        {
            if (sampler.cpuElapsedTime > 0)
                Measure.Custom(sampleGroups[sampler].cpu, sampler.cpuElapsedTime);
            if (sampler.gpuElapsedTime > 0)
                Measure.Custom(sampleGroups[sampler].gpu, sampler.gpuElapsedTime);
            if (sampler.inlineCpuElapsedTime > 0)
                Measure.Custom(sampleGroups[sampler].inlineCPU, sampler.inlineCpuElapsedTime);
        }
    }

    protected static IEnumerable<Type> GetMemoryObjectTypes()
    {
        yield return typeof(RenderTexture);
        yield return typeof(Texture2D);
        yield return typeof(Texture2DArray);
        yield return typeof(Texture3D);
        yield return typeof(CubemapArray);
        yield return typeof(Cubemap);
        yield return typeof(Material);
        yield return typeof(Mesh);
        yield return typeof(Shader);
        yield return typeof(ComputeShader);
    }

    protected IEnumerator ReportMemoryUsage(PerformanceTestSceneSettings sceneSettings, MemoryTestDescription testDescription)
    {
        long totalMemory = 0;
        var data = Resources.FindObjectsOfTypeAll(testDescription.assetType);
        var results = new List<(string name, long size)>();

        // Measure memory
        foreach (var item in data)
        {
            string name = String.IsNullOrEmpty(item.name) ? item.GetType().Name : item.name;
            long currSize = Profiler.GetRuntimeMemorySizeLong(item);

            // There are too many items here so we only keep the one that have a minimun of weight
            if (currSize > sceneSettings.minObjectSize)
                results.Add((name, currSize));

            totalMemory += currSize;
        }

        results.Sort((a, b) => b.size.CompareTo(a.size));

        string resolution = $"{sceneSettings.cameraWidth}x{sceneSettings.cameraHeight}";

        // Report data
        foreach (var result in results)
            Measure.Custom(new SampleGroup(FormatSampleGroupName(k_Memory, result.name, resolution), SampleUnit.Byte, false), result.size);
        Measure.Custom(new SampleGroup(FormatSampleGroupName(k_TotalMemory, testDescription.assetType.Name, resolution), SampleUnit.Byte, false), totalMemory);

        yield return null;
    }
}
