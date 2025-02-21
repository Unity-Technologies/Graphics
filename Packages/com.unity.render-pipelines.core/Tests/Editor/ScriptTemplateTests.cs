using UnityEditor;
using NUnit.Framework;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Tests
{
    class ScriptTemplatesTests
    {
        string[] paths = new string[]
        {
            $"{ScriptTemplates.ScriptTemplatePath}BlitSRP.txt"
        };

        [Test]
        public void ScriptTemplatesExist()
        {
            for (int i = 0; i < paths.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(paths[i]);

                Assert.NotNull(asset);
            }
        }
    }
}
