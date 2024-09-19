using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Rendering.Tests
{
    class RenderGraphViewerTests
    {
        const string kExpectedCurrentFilePath = "Packages/com.unity.render-pipelines.core/Tests/Editor/RenderGraphViewerTests.cs";

        static TestCaseData[] s_TestsCaseDatas =
        {
            new (@"Packages/com.unity.render-pipelines.core/Tests/Editor/RenderGraphViewerTests.cs"),
            new (@"./Packages/com.unity.render-pipelines.core/Tests/Editor/RenderGraphViewerTests.cs"),
            new (@"Packages\com.unity.render-pipelines.core\Tests\Editor\RenderGraphViewerTests.cs"),
            new (@".\Packages\com.unity.render-pipelines.core\Tests\Editor\RenderGraphViewerTests.cs"),
            new (@"Library/PackageCache/com.unity.render-pipelines.core/Tests/Editor/RenderGraphViewerTests.cs"),
            new (@"./Library/PackageCache/com.unity.render-pipelines.core/Tests/Editor/RenderGraphViewerTests.cs"),
            new (@"Library\PackageCache\com.unity.render-pipelines.core\Tests\Editor\RenderGraphViewerTests.cs"),
            new (@".\Library\PackageCache\com.unity.render-pipelines.core\Tests\Editor\RenderGraphViewerTests.cs")
        };

        [Test, TestCaseSource(nameof(s_TestsCaseDatas))]
        public void ScriptAbsolutePathToRelative(string absolutePath)
        {
            Assert.AreEqual(kExpectedCurrentFilePath, RenderGraphViewer.ScriptAbsolutePathToRelative(absolutePath));
        }

        [Test]
        public void CallerFilePathToRelative()
        {
            var absolutePath = GetCallerFilePath();
            var relativePath = RenderGraphViewer.ScriptAbsolutePathToRelative(absolutePath);

            // Use a regex to strip the fingerprint part (e.g., @ec76e1a6c2d6) from the relative path
            var relativePathWithoutFingerprint = Regex.Replace(relativePath, "@[a-zA-Z0-9]+", string.Empty);

            Assert.AreEqual(kExpectedCurrentFilePath, relativePathWithoutFingerprint);
        }

        string GetCallerFilePath([CallerFilePath] string filePath = null) => filePath;

        [Test]
        public void ProjectAssetsFilePathToRelative()
        {
            const string kFileInsideProject = "File.cs";
            var absolutePath = Path.Join(Application.dataPath, kFileInsideProject);
            Assert.AreEqual($"Assets/{kFileInsideProject}", RenderGraphViewer.ScriptAbsolutePathToRelative(absolutePath));
        }
    }
}
