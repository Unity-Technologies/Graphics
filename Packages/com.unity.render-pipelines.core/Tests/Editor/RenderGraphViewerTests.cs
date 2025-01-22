using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Rendering.Tests
{
    // Nested under this namespace otherwise the compiled used the wrong PackageInfo (it used the one found in UnityEditor, despite the using alias).
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;

    class RenderGraphViewerTests
    {
        const string kExpectedCurrentFilePath = "Tests/Editor/RenderGraphViewerTests.cs";

        static IEnumerable<TestCaseData> ScriptPathToAssetPathTestCases()
        {
            var packageInfo = PackageInfo.FindForAssembly(Assembly.GetExecutingAssembly());
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            var relativePackagePath = Path.GetRelativePath(projectPath, packageInfo.resolvedPath);

            var scenarios = new []
            {
                // Script in project
                ( "Assets/File.cs", "Assets/File.cs" ),

                // Script in package (this will work regardless of where the package is located,
                // i.e. in Library, embedded in Packages folder, via "file:<path>" dependency or Git URL)
                ( $"{relativePackagePath}/File.cs".Replace(@"\", "/"), $"{packageInfo.assetPath}/File.cs" ),

                // Unknown path
                ( "Unknown/Path/File.cs", null )
            };

            // Relative paths
            foreach (var (inputPath, expectedResult) in scenarios)
            {
                // Relative paths, Unity separators
                yield return new TestCaseData(inputPath, expectedResult);

                // Relative paths with ./ prefix, as present in MonoScript script path, Unity separators
                yield return new TestCaseData($"./{inputPath}", expectedResult);

                // Absolute paths, Unity separators
                yield return new TestCaseData($"{projectPath}/{inputPath}", expectedResult);
#if PLATFORM_WINDOWS
                // Relative paths, Windows separators
                yield return new TestCaseData(inputPath.Replace("/", @"\"), expectedResult);

                // Relative paths with ./ prefix, as present in MonoScript script path, Windows separators
                yield return new TestCaseData($"./{inputPath}".Replace("/", @"\"), expectedResult);

                // Absolute paths, Windows separators
                yield return new TestCaseData($"{projectPath}/{inputPath}".Replace("/", @"\"), expectedResult);
#endif
            }
        }

        [Test, TestCaseSource(nameof(ScriptPathToAssetPathTestCases))]
        public void ScriptPathToAssetPath(string absoluteOrRelativePath, string expectedResult)
        {
            // expectedResult == null --> input returned untransformed
            Assert.AreEqual(expectedResult ?? absoluteOrRelativePath, RenderGraphViewer.ScriptPathToAssetPath(absoluteOrRelativePath));
        }

        [Test]
        public void CallerFilePathToRelative()
        {
            var absolutePath = GetCallerFilePath();
            var packageInfo = PackageInfo.FindForPackageName("com.unity.render-pipelines.core");
            Assert.IsNotNull(packageInfo);

            var expectedPath = $"{packageInfo.assetPath}/{kExpectedCurrentFilePath}";
            Assert.AreEqual(expectedPath, RenderGraphViewer.ScriptPathToAssetPath(absolutePath));
        }

        string GetCallerFilePath([CallerFilePath] string filePath = null) => filePath;
    }
}
