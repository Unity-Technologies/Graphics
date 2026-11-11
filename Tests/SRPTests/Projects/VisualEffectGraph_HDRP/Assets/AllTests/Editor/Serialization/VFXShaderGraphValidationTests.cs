using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX.UI;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test.Integration
{
    [TestFixture]
    public class VFXShaderGraphValidationTests
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            VFXTestCommon.CloseAllVFXWindow();
        }

        private static readonly Regex k_DiffusionProfileError = new Regex(@".*Diffusion Profile blackboard properties.*");
        private List<string> m_UnexpectedErrors;

        private void CollectUnexpectedErrors(string condition, string stackTrace, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                && !k_DiffusionProfileError.IsMatch(condition))
                m_UnexpectedErrors.Add(condition);
        }

        [TearDown]
        public void Clean()
        {
            if (m_UnexpectedErrors != null)
            {
                Application.logMessageReceived -= CollectUnexpectedErrors;
                m_UnexpectedErrors = null;
            }

            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest, Description("Cover regression UUM-5728")]
        public IEnumerator ShaderGraph_Lit_On_Unlit()
        {
            var reproContent = "Assets/AllTests/Editor/Tests/VFXSerialization_Repro_5728.zip";
            var tempDest = VFXTestCommon.tempBasePath + "/Repro_5728";

            LogAssert.Expect(LogType.Error, new Regex(@".*You must use an unlit vfx master node with an unlit output.*"));
            LogAssert.Expect(LogType.Error, new Regex(@".*Output task missing processor \(Material\).*"));
            LogAssert.Expect(LogType.Error, new Regex(@".*Invalid VFX Particle System.*"));

            System.IO.Compression.ZipFile.ExtractToDirectory(reproContent, tempDest);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            yield return null;

            SceneManagement.EditorSceneManager.OpenScene(tempDest + "/Repro_5728.unity");

            for (int i = 0; i < 4; ++i)
                yield return null;

            SceneManagement.EditorSceneManager.OpenScene("Assets/empty.unity");

            for (int i = 0; i < 4; ++i)
                yield return null;
        }

        [UnityTest, Description("Cover regression UUM-13863")]
        public IEnumerator Crash_On_StoreObject_While_Modifying_SG()
        {
            var reproContent = "Assets/AllTests/Editor/Tests/VFXSerialization_Repro_13863.zip";
            var tempDest = VFXTestCommon.tempBasePath + "/Repro_13863";

            // The repro's unsupported blackboard property logs one error per full compile, and how many compiles a
            // refresh runs is not fixed, so the count can't be asserted. Tolerate any number of that one diagnostic
            // and fail on any other error, which LogAssert alone cannot express.
            m_UnexpectedErrors = new List<string>();
            Application.logMessageReceived += CollectUnexpectedErrors;
            LogAssert.ignoreFailingMessages = true;

            System.IO.Compression.ZipFile.ExtractToDirectory(reproContent, tempDest);

            AssetDatabase.Refresh();
            yield return null;

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(tempDest + "/Repro_13863.vfx");
            Assert.IsNotNull(asset);

            VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0);
            var window = VFXTestCommon.GetWindow(asset);
            window.LoadAsset(asset, null);
            for (int i = 0; i < 4; ++i)
                yield return null;

            var baseFilePath = tempDest + "/Repro_13863_A.shadergraph";
            var backupSGContent = File.ReadAllBytes(baseFilePath);
            var newSGContent = File.ReadAllBytes(tempDest + "/Repro_13863_B.shadergraph");
            Assert.IsNotEmpty(backupSGContent);
            Assert.IsNotEmpty(newSGContent);

            File.WriteAllBytes(baseFilePath, newSGContent);
            AssetDatabase.Refresh();
            for (int i = 0; i < 4; ++i)
                yield return null;

            File.WriteAllBytes(baseFilePath, backupSGContent);
            AssetDatabase.Refresh();

            for (int i = 0; i < 4; ++i)
                yield return null; //Crash is occurring here

            window.Close();
            for (int i = 0; i < 4; ++i)
                yield return null;

            CollectionAssert.IsEmpty(m_UnexpectedErrors, "Unexpected error(s) logged: " + string.Join(" | ", m_UnexpectedErrors));
        }
    }
}
