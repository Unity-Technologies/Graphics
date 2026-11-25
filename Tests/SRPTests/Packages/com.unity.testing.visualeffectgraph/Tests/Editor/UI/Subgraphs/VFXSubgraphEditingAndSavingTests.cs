using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.UI.Subgraphs
{
    [TestFixture]
    public class VFXSubgraphEditingAndSavingTests
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

        [UnityTest, Description("Repro UUM-84307")]
        public IEnumerator Repro_CustomHLSL_In_Subgraph()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_84307.unitypackage";
            var vfxPath = VFXTestCommon.tempBasePath + "/Repro_84307.vfx";

            UnityEditor.AssetPackage.Utility.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets();
            yield return null;

            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsNotNull(vfxAsset);
            var vfxGraph = vfxAsset.GetResource().GetGraph();
            Assert.IsNotNull(vfxGraph);

            var firstContext = vfxGraph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
            Assert.IsNotNull(firstContext);

            var originalCapacity = (uint)firstContext.GetData().GetSettingValue("capacity");
            firstContext.GetData().SetSettingValue("capacity", originalCapacity + 1u);
            Assert.IsTrue(EditorUtility.IsDirty(vfxGraph));
            AssetDatabase.SaveAssets();
            Assert.IsFalse(EditorUtility.IsDirty(vfxGraph));
        }


        [UnityTest]
        public IEnumerator Subgraph_Update_With_Main_Window_Opened()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_Subgraph_Add_Exposed_Input.unitypackage";
            UnityEditor.AssetPackage.Utility.ImportPackageImmediately(packagePath);

            var vfxPathMain = VFXTestCommon.tempBasePath + "/Repro_Subgraph_Main.vfx";
            var vfxPath_A = VFXTestCommon.tempBasePath + "/Repro_Subgraph_A.vfxoperator";
            var vfxPath_B = VFXTestCommon.tempBasePath + "/Repro_Subgraph_B.vfxoperator";

            var vfxPath_A_Content = File.ReadAllText(vfxPath_A);
            var vfxPath_B_Content = File.ReadAllText(vfxPath_B);

            yield return null;

            var rootAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPathMain);
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(rootAsset.GetResource(), null);
            yield return null;

            var vfxGraph = rootAsset.GetResource().GetGraph();
            var subgraph = vfxGraph.children.OfType<VFXSubgraphOperator>().Single();
            Assert.AreEqual(1, subgraph.inputSlots.Count);

            File.WriteAllText(vfxPath_A, vfxPath_B_Content);
            AssetDatabase.Refresh();
            yield return null;
            Assert.AreEqual(2, subgraph.inputSlots.Count);

            File.WriteAllText(vfxPath_A, vfxPath_A_Content);
            AssetDatabase.Refresh();
            yield return null;
            Assert.AreEqual(1, subgraph.inputSlots.Count);

            window.Close();
            yield return null;
        }

        [UnityTest, Description("Corner case discovered while verifying #73468")]
        public IEnumerator ConvertToSubGraph_With_CustomHLSL()
        {
            UnityEditor.AssetPackage.Package.Import("Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_Subgraph_CustomHLSL.unitypackage", false);
            yield return null;

            var scene = SceneManagement.EditorSceneManager.OpenScene("Assets/TmpTests/Repro_Subgraph_CustomHLSL.unity");
            Assert.IsNotNull(scene);
            yield return null;

            var sceneView = SceneView.GetWindow(typeof(SceneView));
            sceneView.position = new Rect(0, 0, 800, 600);
            Assert.AreEqual(1, UnityEngine.VFX.VFXManager.GetComponents().Length);
            var vfx = UnityEngine.VFX.VFXManager.GetComponents()[0];

            var vfxResource = VisualEffectResource.GetResourceAtPath("Assets/TmpTests/Repro_Subgraph_CustomHLSL.vfx");
            Assert.IsNotNull(vfxResource);
            var window = VFXTestCommon.GetWindow(vfxResource, true, true);
            window.LoadAsset(vfxResource.asset, vfx);
            yield return null;
            var maxFrame = 8;
            while (maxFrame-- > 0 && VisualEffectAssetUtility.GetCompilationMode(vfxResource.asset) == VFXCompilationMode.Edition)
                yield return null;
            Assert.IsTrue(maxFrame > 0);

            maxFrame = 16;
            while (maxFrame-- > 0 && vfx.aliveParticleCount == 0)
                yield return null;
            Assert.IsTrue(maxFrame > 0);

            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);

            var blockController = viewController.AllSlotContainerControllers
                .Where(x => x.model.GetParent() is VFXPointOutput).ToArray();
            Assert.AreEqual(1, blockController.Length);

            var nodeController = viewController.AllSlotContainerControllers
                .Where(x => x.model is VFXOperator || x.model is VFXParameter).ToArray();
            Assert.AreEqual(6, nodeController.Length);

            var subgraphName = $"Assets/TmpTests/Repro_Subgraph_CustomHLSL_{Guid.NewGuid()}.vfxblock";
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, blockController.Concat(nodeController), Rect.zero, subgraphName);
            viewController.ApplyChanges();
            yield return null;

            maxFrame = 16;
            while (maxFrame-- > 0 && vfx.aliveParticleCount == 0)
                yield return null;
            Assert.IsTrue(maxFrame > 0);

            window.SaveChanges();
            yield return null;

            window.Close();
            yield return null;
        }
    }
}
