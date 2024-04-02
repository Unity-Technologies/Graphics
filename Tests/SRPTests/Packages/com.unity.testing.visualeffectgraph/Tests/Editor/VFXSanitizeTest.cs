using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXSanitizeTest
    {
        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest]
        public IEnumerator Check_SetCustomAttribute_Sanitize()
        {
            // No assert because if there's at least one error message in the console during the asset import+sanitize the test will fail
            var filePath = "Packages/com.unity.testing.visualeffectgraph/scenes/103_Lit.vfxtmp";
            var graph = VFXTestCommon.CopyTemporaryGraph(filePath);
            for (int i = 0; i < 16; i++)
                yield return null;
            Assert.IsNotNull(graph);
        }

        [UnityTest,
#if VFX_TESTS_HAS_URP
    Ignore("See UUM-66527")
#endif
        ]
        public IEnumerator Insure_Templates_Are_Up_To_Date()
        {
            var allTemplatesGUI = AssetDatabase.FindAssets("t:VisualEffectAsset", new []{ "Packages/com.unity.visualeffectgraph" });
            var templatePath = new List<string>();
            foreach (var guid in allTemplatesGUI)
            {
                var currentPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(currentPath);

                var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(currentPath);
                Assert.IsNotNull(asset);

                var resource = asset.GetResource();
                EditorUtility.SetDirty(resource);
                AssetDatabase.ImportAsset(currentPath);

                templatePath.Add(currentPath);
            }
            AssetDatabase.SaveAssets();
            Assert.AreNotEqual(0, templatePath.Count);
            yield return null;

            using (var process = new System.Diagnostics.Process())
            {
                var rootPath = Path.Combine(Application.dataPath, "../../../../../");
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    FileName = "git",
                    Arguments = "diff Packages/com.unity.visualeffectgraph/**",
                    WorkingDirectory = rootPath
                };

                var outputBuilder = new StringBuilder();
                var errorsBuilder = new StringBuilder();
                process.OutputDataReceived += (_, args) => outputBuilder.AppendLine(args.Data);
                process.ErrorDataReceived += (_, args) => errorsBuilder.AppendLine(args.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                var output = outputBuilder.ToString().TrimEnd();
                var errors = errorsBuilder.ToString().TrimEnd();

                Assert.AreEqual(0, process.ExitCode);
                Assert.AreEqual(string.Empty, errors);
                Assert.AreEqual(string.Empty, output, output);
            }
            yield return null;
        }

    }
}
