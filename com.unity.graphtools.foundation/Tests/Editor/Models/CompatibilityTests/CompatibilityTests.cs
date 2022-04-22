using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public class CompatibilityTests
    {
        const string k_TemporaryAssetDir = "Assets/CompatibilityTests";
        const string k_TemporaryAssetName = "TemporaryAsset";
        static readonly string k_TemporaryAssetPath = $"{k_TemporaryAssetDir}/{k_TemporaryAssetName}.asset";

        const string k_TemporaryAssetName2 = "TemporaryAsset2";
        static readonly string k_TemporaryAssetPath2 = $"{k_TemporaryAssetDir}/{k_TemporaryAssetName2}.asset";

#if UNITY_2021_2_OR_NEWER
        const string k_HiddenDirName = "Assets_2021_2_OR_NEWER~";
#else
        const string k_HiddenDirName = "Assets_older~";
#endif
        const string k_CurrentVersionAssetName = "CurrentVersion";
        static readonly string k_AssetDir = "Packages/com.unity.graphtools.foundation/Tests/Editor/Models/CompatibilityTests/TestAssets";
        static readonly string k_TmpCurrentVersionAssetPath = $"{k_TemporaryAssetDir}/{k_CurrentVersionAssetName}.asset";
        static readonly string k_CurrentVersionAssetPath = $"{k_AssetDir}/{k_HiddenDirName}/{k_CurrentVersionAssetName}.asset";
        static readonly string k_PreviousVersionsDirectoryPath = $"{k_AssetDir}/{k_HiddenDirName}/PreviousVersions";

        [MenuItem("internal:GTF/Generate Compatibility Test Reference Asset")]
        public static void GenerateCompatibilityTestLoadReferenceAsset()
        {
            CompatibilityGraphModelBuilder.CreateAsset(k_CurrentVersionAssetName, k_TmpCurrentVersionAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(new[] { k_TmpCurrentVersionAssetPath });
            FileUtil.ReplaceFile(k_TmpCurrentVersionAssetPath, k_CurrentVersionAssetPath);
            AssetDatabase.DeleteAsset(k_TmpCurrentVersionAssetPath);
        }

        static IEnumerable EnumerateHiddenAssetsPath()
        {
            yield return k_CurrentVersionAssetPath;

            var dir = new DirectoryInfo(k_PreviousVersionsDirectoryPath);
            foreach (var f in dir.GetFiles("*.asset"))
            {
                yield return $"{k_PreviousVersionsDirectoryPath}/{f.Name}";
            }
        }

        [Test, Description("Checks that loading a previously generated asset gives the same graph as generating the same graph in C#.")]
        [TestCaseSource(nameof(EnumerateHiddenAssetsPath))]
        public void LoadingAssetMakesSameGraph(string hiddenAssetPath)
        {
            if (!Directory.Exists(k_TemporaryAssetDir))
                Directory.CreateDirectory(k_TemporaryAssetDir);
            var tmpCurrentAssetName = k_TmpCurrentVersionAssetPath;
            FileUtil.ReplaceFile(hiddenAssetPath, tmpCurrentAssetName);
            AssetDatabase.ImportAsset(tmpCurrentAssetName);
            AssetDatabase.ForceReserializeAssets(new[] { tmpCurrentAssetName });
            try
            {
                var expected = CompatibilityGraphModelBuilder.CreateAsset(k_TemporaryAssetName, k_TemporaryAssetPath);

                var actual = AssetDatabase.LoadAssetAtPath<GraphAsset>(tmpCurrentAssetName);
                GraphChecks.AssertIsGraphAsExpected(expected.GraphModel, actual.GraphModel);
            }
            finally
            {
                AssetDatabase.DeleteAsset(k_TemporaryAssetPath);
                AssetDatabase.DeleteAsset(tmpCurrentAssetName);
            }
        }

        [Test, Description("Checks that serializing a graph gives the same file as before.")]
        public void SavingAssetSavesSameFile()
        {
            Assert.IsTrue(File.Exists(k_CurrentVersionAssetPath), "Generate reference asset using menu item 'GTF/Generate Compatibility Test Reference Asset'.");

            var prevText = File.ReadAllText(k_CurrentVersionAssetPath);

#if UNITY_2021_2_OR_NEWER
            prevText = Regex.Replace(prevText,
                $"^((\\s|-)+)rid: [0-9]+$",
                $"$1rid: 0",
                RegexOptions.Multiline);
#endif

            string newText;
            try
            {
                if (!Directory.Exists(k_TemporaryAssetDir))
                    Directory.CreateDirectory(k_TemporaryAssetDir);
                CompatibilityGraphModelBuilder.CreateAsset(k_TemporaryAssetName, k_TemporaryAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ForceReserializeAssets(new[] { k_TemporaryAssetPath });

                newText = File.ReadAllText(k_TemporaryAssetPath);
            }
            finally
            {
                AssetDatabase.DeleteAsset(k_TemporaryAssetPath);
            }

            newText = Regex.Replace(newText,
                $"^(\\s*)m_Name: {k_TemporaryAssetName}(.*)$",
                $"$1m_Name: {k_CurrentVersionAssetName}$2",
                RegexOptions.Multiline);

#if UNITY_2021_2_OR_NEWER
            newText = Regex.Replace(newText,
                $"^((\\s|-)+)rid: [0-9]+$",
                $"$1rid: 0",
                RegexOptions.Multiline);
#endif

            var prevLen = prevText.Length;
            var newLen = newText.Length;
            int i = 0;
            int j = 0;
            int lineNum = 1;
            int colNum = 1;

            void IncrementI()
            {
                if (prevText[i] == '\n')
                {
                    lineNum++;
                    colNum = 1;
                }
                else
                {
                    colNum++;
                }

                i++;
            }

            while (true)
            {
                var initialI = i;
                var initialJ = j;

                while (i < prevLen && j < newLen && prevText[i] == newText[j])
                {
                    IncrementI();
                    j++;
                }

                while (i < prevLen && char.IsWhiteSpace(prevText[i]))
                    IncrementI();

                while (j < newLen && char.IsWhiteSpace(newText[j]))
                    j++;

                if (i == prevLen && j == newLen)
                    break;

                if (initialI == i || initialJ == j)
                {
                    var prevDiff = prevText.Substring(Math.Max(0, i - 40), 80);
                    var newDiff = newText.Substring(Math.Max(0, i - 40), 80);

                    Assert.Fail($"Assets content are not equal." +
                        $"\nPrevious length: {prevLen}." +
                        $"\nNew length: {newLen}." +
                        $"\nThey differ at line {lineNum} column {colNum}" +
                        $"\n{prevDiff}" +
                        $"\n------" +
                        $"\n{newDiff}");
                }
            }
        }

        [Test]
        public void SaveAndLoadGraphAssetGivesSameGraph()
        {
            try
            {
                var expected = CompatibilityGraphModelBuilder.CreateAsset(k_TemporaryAssetName, k_TemporaryAssetPath);

                CompatibilityGraphModelBuilder.CreateAsset(k_TemporaryAssetName2, k_TemporaryAssetPath2);
                AssetDatabase.SaveAssets();
                AssetDatabase.ForceReserializeAssets(new[] { k_TemporaryAssetPath2 });
                AssetDatabase.ImportAsset(k_TemporaryAssetPath2, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var actual = AssetDatabase.LoadAssetAtPath<GraphAsset>(k_TemporaryAssetPath2);

                Assert.AreNotSame(expected, actual);
                GraphChecks.AssertIsGraphAsExpected(expected.GraphModel, actual.GraphModel);
            }
            finally
            {
                AssetDatabase.DeleteAsset(k_TemporaryAssetPath);
                AssetDatabase.DeleteAsset(k_TemporaryAssetPath2);
            }
        }
    }
}
