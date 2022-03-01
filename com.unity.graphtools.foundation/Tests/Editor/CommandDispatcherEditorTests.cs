using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class CommandDispatcherEditorTests
    {
        const string scenePath = "Assets/test.unity";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(scenePath);
        }

        [Test]
        public void TestSavingSceneDoesNotAssert()
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, scenePath);

            GameObject go = new GameObject("miaou");

            EditorSceneManager.SaveScene(scene, scenePath);
            //The test will fail if some asserts are added to the console.

        }
    }
}
