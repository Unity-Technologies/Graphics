using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.Universal.Tests
{
    [TestFixture]
    class UILitShaderRemapTests
    {
        const string k_MaterialPath = "Assets/UILitShaderRemapTest.mat";
        const string k_PlaceholderShader = "Universal Render Pipeline/Unlit";
        static readonly Regex k_ShaderReference = new Regex(@"m_Shader: \{[^}]*\}");

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(k_MaterialPath);
        }

        [TestCase(10764, "UI/Lit/Transparent")]
        [TestCase(10765, "UI/Lit/Bumped")]
        [TestCase(10766, "UI/Lit/Detail")]
        [TestCase(10767, "UI/Lit/Refraction")]
        [TestCase(10768, "UI/Lit/Refraction Detail")]
        public void MaterialReferencingBuiltinUILitShader_LoadsPackageShader(int builtinFileID, string shaderName)
        {
            AssetDatabase.CreateAsset(new Material(Shader.Find(k_PlaceholderShader)), k_MaterialPath);

            // Point the saved material at the shader as it was referenced when UI/Lit was a built-in resource
            string text = File.ReadAllText(k_MaterialPath);
            string builtinReference = $"m_Shader: {{fileID: {builtinFileID}, guid: 0000000000000000f000000000000000, type: 0}}";
            if (!k_ShaderReference.IsMatch(text))
                Assert.Ignore("The test material was not saved as text, so its shader reference cannot be rewritten.");
            File.WriteAllText(k_MaterialPath, k_ShaderReference.Replace(text, builtinReference, 1));
            AssetDatabase.ImportAsset(k_MaterialPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            var material = AssetDatabase.LoadAssetAtPath<Material>(k_MaterialPath);

            Assert.IsNotNull(material, "Could not load the test material.");
            Assert.IsNotNull(material.shader, "The material has no shader.");
            Assert.AreEqual(shaderName, material.shader.name);
        }
    }
}
