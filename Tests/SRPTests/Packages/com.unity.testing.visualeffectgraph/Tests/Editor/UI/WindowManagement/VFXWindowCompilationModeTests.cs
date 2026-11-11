using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.UI.WindowManagement
{
    [TestFixture]
    public class VFXWindowCompilationModeTests
    {
        public bool m_GenerateShadersWithDebugSymbols;

        [OneTimeSetUp]
        public void Setup()
        {
            m_GenerateShadersWithDebugSymbols = VFXViewPreference.generateShadersWithDebugSymbols;

            EditorPrefs.SetBool(VFXGlobalImportSettings.generateShadersWithDebugSymbolsKey, false);
            VFXViewPreference.SetDirty();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.DeleteAllTemporaryGraph();

            EditorPrefs.SetBool(VFXGlobalImportSettings.generateShadersWithDebugSymbolsKey, m_GenerateShadersWithDebugSymbols);
            VFXViewPreference.SetDirty();
        }

        [TearDown]
        public void TearDown()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.CloseAllVFXWindow();
        }

        [UnityTest]
        public IEnumerator Check_Default_Edition_Mode_While_Opening_Window()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(AssetDatabase.GetAssetPath(graph));

            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.GetResource(), null);
            for (int i = 0; i < 8; ++i)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(asset) == VFXCompilationMode.Edition)
                    break;
                yield return null;
            }

            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(asset));
            window.Close();
            yield return null;

            Assert.AreEqual(VFXCompilationMode.Runtime, VisualEffectAssetUtility.GetCompilationMode(asset));
        }

        [UnityTest]
        public IEnumerator Check_Toggle_Behavior()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();

            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.GetResource(), null);
            for (int i = 0; i < 8; ++i)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset) == VFXCompilationMode.Edition)
                    break;

                yield return null;
            }

            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset));
            var sourceInit = graph.GetResource().GetShaderSource(1);
            Assert.IsTrue(sourceInit.Contains("CSMain"));
            Assert.IsFalse(sourceInit.Contains("#pragma enable_d3d11_debug_symbols"));

            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset));
            Assert.AreEqual(false, window.graphView.GetForceShaderDebugSymbols());
            window.graphView.ToggleForceShaderDebugSymbols();
            Assert.AreEqual(true, window.graphView.GetForceShaderDebugSymbols());
            yield return null;

            var sourceAfter = graph.GetResource().GetShaderSource(1);
            Assert.IsTrue(sourceAfter.Contains("CSMain"));
            Assert.IsTrue(sourceAfter.Contains("#pragma enable_d3d11_debug_symbols"));

            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset));
            Assert.AreEqual(true, window.graphView.GetForceShaderDebugSymbols());
            window.graphView.ToggleForceShaderDebugSymbols();
            Assert.AreEqual(false, window.graphView.GetForceShaderDebugSymbols());
            yield return null;

            var sourceRestore = graph.GetResource().GetShaderSource(1);
            Assert.IsTrue(sourceRestore.Contains("CSMain"));
            Assert.IsFalse(sourceRestore.Contains("#pragma enable_d3d11_debug_symbols"));
            Assert.AreEqual(sourceRestore, sourceInit);
            yield return null;

            window.Close();
        }
    }
}
