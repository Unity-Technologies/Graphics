using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System;

[CreateAssetMenu]
public class TestSceneAsset : ScriptableObject
{
    [Serializable]
    public class SceneData
    {
        public string                   scene;
        public string                   scenePath;
        public bool                     enabled;
    }

    // Store the name of the scenes so we can load them at runtime
    public List<SceneData> performanceCounterScenes = new List<SceneData>();
    public List<SceneData> memoryTestScenes = new List<SceneData>();
    public List<SceneData> buildTestScenes = new List<SceneData>();
    
    public List<HDRenderPipelineAsset> performanceCounterHDAssets = new List<HDRenderPipelineAsset>();
    public List<HDRenderPipelineAsset> memoryTestHDAssets = new List<HDRenderPipelineAsset>();
    public List<HDRenderPipelineAsset> buildHDAssets = new List<HDRenderPipelineAsset>();

    public IEnumerable<SceneData> GetAllScenes()
    {
        foreach (var scene in performanceCounterScenes)
            yield return scene;
        foreach (var scene in memoryTestScenes)
            yield return scene;
        foreach (var scene in buildTestScenes)
            yield return scene;
    }
}
