using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Unity.PerformanceTesting;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

internal class HDRP_PerformaceTests : IPrebuildSetup
{
    protected static readonly int WarmupCount = 10;
    protected static readonly int MeasurementCount = 100;
    protected const int GlobalTimeout = 120 * 1000;

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
        var testScenes = Resources.Load<TestSceneAsset>("TestScenes");

        testScenes.testScenes = UnityEditor.EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();

        UnityEditor.EditorUtility.SetDirty(testScenes);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    public static IEnumerable<string> GetScenesInBuildSettings()
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

#if UNITY_EDITOR
        sceneCount = UnityEditor.EditorBuildSettings.scenes.Length;
        sceneCount = UnityEditor.EditorBuildSettings.scenes.Length;
        foreach (var scene in UnityEditor.EditorBuildSettings.scenes)
            yield return Path.GetFileNameWithoutExtension(scene.path);
#else
        var testScenes = Resources.Load<TestSceneAsset>("TestScenes");
        foreach (var scene in testScenes.testScenes)
            yield return scene;
#endif
    }

    [Timeout(GlobalTimeout), Version("1"), UnityTest, Performance]
    public IEnumerator Test(
        [ValueSource("GetScenesInBuildSettings")] string sceneName,
        [ValueSource("configs")] Config config)
    {
        SceneManager.LoadScene(sceneName);

        // Wait one frame for the scene to finish loading:
        yield return null;

        var camera = GameObject.FindObjectOfType<Camera>();
        var hdCamera = HDCamera.GetOrCreate(camera, 0); // We don't support XR for now


        SetupCamera(config, hdCamera);

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
            using (Measure.Scope())
            {
                Measure.Custom(gBufferGPU, g.gpuElapsedTime);
                Measure.Custom(gBufferCPU, g.cpuElapsedTime);
                Measure.Custom(cameraGPU, hdCamera.profilingSampler.gpuElapsedTime);
                Measure.Custom(cameraCPU, hdCamera.profilingSampler.cpuElapsedTime);
                Measure.Custom(sampleCount, hdCamera.profilingSampler.gpuSampleCount);
                yield return null;
            }
        }

        hdCamera.profilingSampler.enableRecording = false;
        g.enableRecording = false;
    }

    static void SetupCamera(Config config, HDCamera hdCamera)
    {
        var additionalData = hdCamera.camera.GetComponent<HDAdditionalCameraData>();
        additionalData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.LitShaderMode, true);

        switch (config)
        {
            case Config.Deferred:
                additionalData.renderingPathCustomFrameSettings.litShaderMode = LitShaderMode.Deferred;
                break;
            case Config.Forward:
                additionalData.renderingPathCustomFrameSettings.litShaderMode = LitShaderMode.Forward;
                break;
        }
    }
}