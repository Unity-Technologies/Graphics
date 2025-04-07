using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

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

        static readonly Foldout k_FoldoutElement = new Foldout();
        static IEnumerable<TestCaseData> SearchFilterTestCases()
        {
            // To simplify the test, we use square brackets "[]" to indicate search match highlight.
            string FormatWithSearchTag(string str) =>
                str.Replace("[", RenderGraphViewer.k_SelectionColorBeginTag).Replace("]", RenderGraphViewer.k_SelectionColorEndTag);

            TestCaseData MakeTestCase(string content, string searchString, string result, bool isMatch)
            {
                var dict = new Dictionary<VisualElement, List<TextElement>>();
                dict[k_FoldoutElement] = new List<TextElement>() { new() { text = content } };
                var testCase = new TestCaseData(dict, searchString, new List<string> { FormatWithSearchTag(result) }, isMatch);
                testCase.SetName($"Searching \"{content}\" for \"{searchString}\" results in \"{result}\" (match={isMatch})");
                return testCase;
            }

            // Basic string matches
            yield return MakeTestCase("Text", "Te", "[Te]xt", true);
            yield return MakeTestCase("Text", "xt", "Te[xt]", true);
            yield return MakeTestCase("Text", "Foo", "Text", false);
            yield return MakeTestCase("Text", "", "Text", true);
            // Only first match per string is highlighted
            yield return MakeTestCase("Text Text Text", "Text", "[Text] Text Text", true);
            // Verify tags inside target text don't get broken
            yield return MakeTestCase("<b>Bold Text</b>", "Text", "<b>Bold [Text]</b>", true);
            yield return MakeTestCase("<b>Bold Text</b>", "b", "<b>[B]old Text</b>", true);
            yield return MakeTestCase("<b>No Match Here</b>", "b", "<b>No Match Here</b>", false);
            yield return MakeTestCase("<i>Multiple</i> <i>Tags</i>", "i", "<i>Mult[i]ple</i> <i>Tags</i>", true);
            yield return MakeTestCase("<i>Many</i> <i>Tags</i>", "i", "<i>Many</i> <i>Tags</i>", false);
            yield return MakeTestCase("Text<br>On<br>Three Lines", "On", "Text<br>[On]<br>Three Lines", true);

        }

        [Test, TestCaseSource(nameof(SearchFilterTestCases))]
        public void SearchFiltering(Dictionary<VisualElement, List<TextElement>> content, string searchString, List<string> expectedResults, bool isMatch)
        {
            RenderGraphViewer.PerformSearch(content, searchString);

            var elements = content[k_FoldoutElement];
            for (int i = 0; i < elements.Count; i++)
            {
                Assert.AreEqual(elements[i].text, expectedResults[i]);
                Assert.AreEqual(k_FoldoutElement.style.display.value, isMatch ? DisplayStyle.Flex : DisplayStyle.None);
            }
        }
    }
}
