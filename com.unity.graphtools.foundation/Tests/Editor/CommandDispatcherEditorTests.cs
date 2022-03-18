using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class CommandDispatcherEditorTests
    {
        const string k_ScenePath = "Assets/test.unity";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(k_ScenePath);
        }

        [Test]
        public void TestSavingSceneDoesNotAssert()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, k_ScenePath);

            var _ = new GameObject("miaou");

            EditorSceneManager.SaveScene(scene, k_ScenePath);
            //The test will fail if some asserts are added to the console.
        }
    }
}
