using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXViewWindowTest
    {
        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest]
        public IEnumerator Check_Tab_Attachment_Behavior()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            while (EditorWindow.HasOpenInstances<VFXViewWindow>())
                EditorWindow.GetWindow<VFXViewWindow>().Close();

            while (EditorWindow.HasOpenInstances<SceneView>())
                EditorWindow.GetWindow<SceneView>().Close();

            yield return null;
            var sceneView = SceneView.GetWindow(typeof(SceneView));
            sceneView.position = new Rect(0, 0, 800, 600);
            yield return null;
            Assert.IsFalse(sceneView.docked);

            Assert.IsTrue(sceneView.m_Parent is DockArea);
            var dockArea = sceneView.m_Parent as DockArea;
            Assert.AreEqual(1, dockArea.m_Panes.Count);

            var emptyVFX = VFXViewWindow.GetWindow((VFXGraph)null, true, true);
            yield return null;
            Assert.AreNotEqual(null, emptyVFX);

            Assert.AreEqual(emptyVFX.m_Parent, sceneView.m_Parent);
            Assert.AreEqual(2, dockArea.m_Panes.Count);
            Assert.IsTrue(emptyVFX.hasFocus);

            var dummyVFX = VFXTestCommon.MakeTemporaryGraph();
            var actualVFX = VFXViewWindow.GetWindow(dummyVFX, true, true);
            yield return null;
            Assert.AreEqual(actualVFX, emptyVFX); //We are supposed to reuse the empty view
            Assert.AreEqual(actualVFX.m_Parent, sceneView.m_Parent);
            Assert.AreEqual(2, dockArea.m_Panes.Count);
            Assert.IsTrue(actualVFX.hasFocus);

            actualVFX.Close();
            yield return null;
            Assert.AreEqual(1, dockArea.m_Panes.Count);

            yield return null;
        }

        [UnityTest, Description("Repro UUM-84307")]
        public IEnumerator Repro_CustomHLSL_In_Subgraph()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_84307.unitypackage";
            var vfxPath = VFXTestCommon.tempBasePath + "/Repro_84307.vfx";

            AssetDatabase.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets();
            yield return null;

            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsNotNull(vfxAsset);
            var vfxGraph = vfxAsset.GetOrCreateResource().GetOrCreateGraph();
            Assert.IsNotNull(vfxGraph);

            var firstContext = vfxGraph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
            Assert.IsNotNull(firstContext);

            var originalCapacity = (uint)firstContext.GetData().GetSettingValue("capacity");
            firstContext.GetData().SetSettingValue("capacity", originalCapacity + 1u);
            Assert.IsTrue(EditorUtility.IsDirty(vfxGraph));
            AssetDatabase.SaveAssets();
            Assert.IsFalse(EditorUtility.IsDirty(vfxGraph));
        }
    }
}
