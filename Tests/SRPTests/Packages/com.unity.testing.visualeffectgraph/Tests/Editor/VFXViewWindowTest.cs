using System.Collections;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;

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
    }
}
