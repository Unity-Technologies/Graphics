using System.IO;
using System.Runtime.CompilerServices;
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
            Assert.AreEqual(kExpectedCurrentFilePath, RenderGraphViewer.ScriptAbsolutePathToRelative(absolutePath));
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
