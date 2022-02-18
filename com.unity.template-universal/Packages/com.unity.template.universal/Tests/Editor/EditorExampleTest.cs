using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
//using UnityEngine.Experimental.Rendering.LightweightPipeline;
//using UnityEditor.ShaderGraph;
using NUnit.Framework;
using System.Collections;

class EditorExampleTest
{
    [Test]
    public void EditorSampleTestSimplePasses()
    {
        // Use the Assert class to test conditions.
        //LightweightPipelineAsset lightWeighttest = new LightweightPipelineAsset();
        //AssetDatabase.CreateAsset(lightWeighttest, "Assets/Settings/lwTest.asset");
        //CreatePBRShaderGraph.CreateMaterialGraph();
    }

    // A UnityTest behaves like a coroutine in PlayMode
    // and allows you to yield null to skip a frame in EditMode
    [UnityTest]
    public IEnumerator EditorSampleTestWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // yield to skip a frame
        yield return null;
    }
}
