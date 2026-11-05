using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

class ClusterDataTests
{
    const string k_SceneName = "ClusterDataReadback";
    const string k_ForwardPlusRenderer = "ForwardPlusClusterDataRenderer";
    const string k_DeferredPlusRenderer = "DeferredPlusClusterDataRenderer";
    const int k_RenderedFrames = 8;

    [Description("The cluster data shaders read matches what the clustering jobs produced for each camera, including when two cameras with different lights render in the same frame.")]
    [UnityTest]
    public IEnumerator ClusterData_ReadByShaders_MatchesClusteringResult(
        [Values(k_ForwardPlusRenderer, k_DeferredPlusRenderer)] string rendererName)
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
            Assert.Ignore("The test reads the cluster data back with AsyncGPUReadback.");

        var rendererIndex = ClusterDataReadbackFeature.FindRendererIndex(rendererName);
        Assert.That(rendererIndex, Is.GreaterThanOrEqualTo(0), $"Renderer {rendererName} is not in the pipeline asset.");
        if (!ClusterDataReadbackFeature.RendererUsesClustering(rendererIndex))
            Assert.Ignore($"Renderer {rendererName} does not use the cluster light loop on this platform.");

        ClusterDataReadbackFeature.ResetResults();
        yield return SceneManager.LoadSceneAsync(k_SceneName, LoadSceneMode.Additive);
        foreach (var root in SceneManager.GetSceneByName(k_SceneName).GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                camera.GetUniversalAdditionalCameraData().SetRenderer(rendererIndex);
        for (var i = 0; i < k_RenderedFrames; i++)
            yield return null;
        yield return SceneManager.UnloadSceneAsync(k_SceneName);

        AsyncGPUReadback.WaitAllRequests();
        yield return null;

        var failures = ClusterDataReadbackFeature.failures;
        Assert.IsEmpty(failures, string.Join("\n", failures.GetRange(0, Mathf.Min(10, failures.Count))));

        var hashes = ClusterDataReadbackFeature.expectedHashPerCamera;
        Assert.That(hashes.Count, Is.EqualTo(2), "Expected cluster data from both cameras.");
        Assert.That(new HashSet<int>(hashes.Values).Count, Is.EqualTo(2), "Both cameras produced the same cluster data, so data from the wrong camera would go unnoticed.");
        var wordCounts = ClusterDataReadbackFeature.expectedWordCountPerCamera;
        Assert.That(new HashSet<int>(wordCounts.Values).Count, Is.EqualTo(2), "Both cameras upload the same number of words, so an upload of the wrong size would go unnoticed.");
        Assert.That(ClusterDataReadbackFeature.checkCount, Is.GreaterThanOrEqualTo(k_RenderedFrames), "Too few frames were checked.");
    }
}
