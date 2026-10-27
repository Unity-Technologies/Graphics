using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class NativeTextureDependencyTests
    {
        static string sourceDirectoryPath => Application.dataPath + "/../NativeTextureDependencyGraphs";
        static string targetDirectoryPath => Application.dataPath + "/Testing/NativeTextureDependencyGraphs";
        const string k_TargetUnityDirectoryPath = "Assets/Testing/NativeTextureDependencyGraphs";

        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(k_TargetUnityDirectoryPath);
        }

        // Native assets import after Shader Graph assets in a refresh, so the Sub Graph first imports before its texture
        // and the graph first imports before the Sub Graph is up to date; both must be reimported without errors.
        [Test]
        public void ImportWithNativeTextureDefault_RecordsTextureDependencyWithoutErrors()
        {
            Directory.CreateDirectory(targetDirectoryPath);
            foreach (var sourceFilePath in Directory.GetFiles(sourceDirectoryPath))
                File.Copy(sourceFilePath, targetDirectoryPath + "/" + Path.GetFileName(sourceFilePath));

            AssetDatabase.Refresh();

            var metadata = AssetDatabase.LoadAllAssetsAtPath(k_TargetUnityDirectoryPath + "/NativeTextureSubGraph.shadersubgraph")
                .OfType<ShaderSubGraphMetadata>()
                .Single();
            var dependencyPaths = metadata.assetDependencies.Select(d => AssetDatabase.GetAssetPath(d.asset));
            Assert.That(dependencyPaths, Does.Contain(k_TargetUnityDirectoryPath + "/NativeTexture.asset"));

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(k_TargetUnityDirectoryPath + "/NativeTextureGraph.shadergraph");
            Assert.IsNotNull(shader);
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
        }
    }
}
