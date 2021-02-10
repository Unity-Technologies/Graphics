using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Rendering;
using static PerformanceMetricNames;

[CreateAssetMenu(menuName = "Testing/Performance Test Description")]
public class TestSceneAsset : ScriptableObject
{
    [Serializable]
    public class SceneData
    {
        public string                   scene;
        public string                   sceneLabels;
        public string                   scenePath;
        public bool                     enabled;
    }

    [Serializable]
    public class SRPAssetData
    {
        public RenderPipelineAsset  asset;
        public string               assetLabels;
        public string               alias; // reference named used in the test
    }

    [Serializable]
    public class TestSuiteData
    {
        public List<SceneData>      scenes;
        public List<SRPAssetData>   srpAssets;

        public IEnumerable<(SceneData sceneData, SRPAssetData assetData)> GetTestList()
        {
            foreach (var srpAsset in srpAssets)
                foreach (var scene in scenes)
                    if (scene.enabled)
                        yield return (scene, srpAsset);
        }
    }

    // Store the name of the scenes so we can load them at runtime
    public TestSuiteData    counterTestSuite = new TestSuiteData();
    public TestSuiteData    memoryTestSuite = new TestSuiteData();
    public TestSuiteData    buildTestSuite = new TestSuiteData();

    public List<SRPAssetData> srpAssetAliases = new List<SRPAssetData>();

    public IEnumerable<(SceneData sceneData, SRPAssetData assetData)> GetAllTests()
    {
        foreach (var test in counterTestSuite.GetTestList())
            yield return test;
        foreach (var test in memoryTestSuite.GetTestList())
            yield return test;
        foreach (var test in buildTestSuite.GetTestList())
            yield return test;
    }

    public string GetScenePath(string sceneName) => GetAllTests().FirstOrDefault(s => s.sceneData.scene == sceneName).sceneData?.scenePath;

    public string GetSRPAssetAlias(RenderPipelineAsset srpAsset) => srpAssetAliases.Where(a => a.asset == srpAsset).FirstOrDefault()?.alias;
}

public struct CounterTestDescription
{
    public TestSceneAsset.SceneData    sceneData;
    public TestSceneAsset.SRPAssetData  assetData;
    public override string ToString()
    {
        var formatTestName = PerformanceTestUtils.FormatTestName(sceneData.scene, sceneData.sceneLabels,
            String.IsNullOrEmpty(assetData.alias) ? assetData.asset?.name ?? "Builtin" : assetData.alias, assetData.assetLabels,
            k_Default);
        return formatTestName;
    }
}
public struct MemoryTestDescription
{
    public TestSceneAsset.SceneData     sceneData;
    public TestSceneAsset.SRPAssetData  assetData;
    public Type                         assetType;
    public override string ToString()
        => PerformanceTestUtils.FormatTestName(sceneData.scene, sceneData.sceneLabels, String.IsNullOrEmpty(assetData.alias) ? assetData.asset?.name ?? "Builtin" : assetData.alias, assetData.assetLabels, assetType.Name);
}
public struct BuildTestDescription
{
    public TestSceneAsset.SceneData     sceneData;
    public TestSceneAsset.SRPAssetData   assetData;
    public string                       testName;
    public override string ToString()
        => PerformanceTestUtils.FormatTestName(sceneData.scene, sceneData.sceneLabels, String.IsNullOrEmpty(assetData.alias) ? assetData.asset?.name ?? "Builtin" : assetData.alias, assetData.assetLabels, testName);
}