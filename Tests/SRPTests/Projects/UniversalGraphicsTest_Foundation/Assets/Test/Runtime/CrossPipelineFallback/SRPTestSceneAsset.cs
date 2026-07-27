using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[CreateAssetMenu(fileName = "SRPTestSceneSO", menuName = "Graphics Test Framework/SRP Test Scene Asset")]
public class SRPTestSceneAsset : ScriptableObject
{
    [Serializable]
    public class TestData
    {
        public bool enabled = true;
        public Object      scene;
        public List<RenderPipelineAsset>   srpAssets;
        public string path;
    }

    public List<TestData> testDatas = new List<TestData>();
}
