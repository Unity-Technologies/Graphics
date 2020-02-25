using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Linq;

[CreateAssetMenu]
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
    public class HDAssetData
    {
        public HDRenderPipelineAsset    asset;
        public string                   assetLabels;
        public string                   alias; // reference named used in the test
    }

    [Serializable]
    public class TestSuiteData
    {
        public List<SceneData>      scenes;
        public List<HDAssetData>    hdAssets;

        public IEnumerable<(SceneData sceneData, HDAssetData assetData)> GetTestList()
        {
            foreach (var hdAsset in hdAssets)
                foreach (var scene in scenes)
                    if (scene.enabled)
                        yield return (scene, hdAsset);
        }
    }

    // Store the name of the scenes so we can load them at runtime
    public TestSuiteData    counterTestSuite = new TestSuiteData();
    public TestSuiteData    memoryTestSuite = new TestSuiteData();
    public TestSuiteData    buildTestSuite = new TestSuiteData();

    public List<HDAssetData> hdAssetAliases = new List<HDAssetData>();

    public IEnumerable<(SceneData sceneData, HDAssetData assetData)> GetAllTests()
    {
        foreach (var test in counterTestSuite.GetTestList())
            yield return test;
        foreach (var test in memoryTestSuite.GetTestList())
            yield return test;
        foreach (var test in buildTestSuite.GetTestList())
            yield return test;
    }

    public string GetScenePath(string sceneName) => GetAllTests().FirstOrDefault(s => s.sceneData.scene == sceneName).sceneData?.scenePath;

    public string GetHDAssetAlias(HDRenderPipelineAsset hdAsset) => hdAssetAliases.Where(a => a.asset == hdAsset).FirstOrDefault()?.alias;
}
