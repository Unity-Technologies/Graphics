using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        [SerializeField] private VisualEffectAsset m_Domain_Reload_With_VFX_Live_In_Scene_Asset;
        [SerializeField] private VFXGraph m_Domain_Reload_With_VFX_Live_In_Scene_Graph;

        [UnityTest, Description("Cover UUM-112719")]
        public IEnumerator Domain_Reload_With_VFX_Live_In_Scene()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();

            var sceneView = SceneView.GetWindow(typeof(SceneView));
            sceneView.position = new Rect(0, 0, 800, 600);

            var vfxGraph = VFXTestCommon.CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/02_Simple_Loop.vfx");
            m_Domain_Reload_With_VFX_Live_In_Scene_Graph = vfxGraph;
            Assert.IsNotNull(vfxGraph);
            yield return null;

            var mainObjectName = "VFX_Test_Main_Object";
            var mainObject = new GameObject(mainObjectName);

            var mainCameraName = "VFX_Test_Main_Camera";
            var mainCamera = new GameObject(mainCameraName);
            var camera = mainCamera.AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(mainObject.transform.position);

            var vfxComponent = mainObject.AddComponent<VisualEffect>();
            m_Domain_Reload_With_VFX_Live_In_Scene_Asset = vfxGraph.GetResource().asset;

            var previewAssets = VFXTestCommon.GetPreviewAssets(vfxGraph);
            Assert.AreEqual(0, previewAssets.Length);

            vfxComponent.visualEffectAsset = vfxGraph.GetResource().asset;

            Assert.AreEqual(VFXCompilationMode.Runtime, VisualEffectAssetUtility.GetCompilationMode(m_Domain_Reload_With_VFX_Live_In_Scene_Asset));
            var window = VFXViewWindow.GetWindow(vfxGraph, true, true);
            window.LoadResource(vfxGraph.GetResource(), vfxComponent);
            
            for (int i = 0; i < 4; ++i)
                yield return null;

            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(m_Domain_Reload_With_VFX_Live_In_Scene_Asset));

            Assert.IsFalse(EditorUtility.IsDirty(vfxGraph));
            var output = vfxGraph.children.OfType<VFXAbstractRenderedOutput>().Single();
            var block = output.children.First();
            block.activationSlot.value = false;
            Assert.IsTrue(EditorUtility.IsDirty(vfxGraph));
            yield return null;

            previewAssets = VFXTestCommon.GetPreviewAssets(vfxGraph);
            Assert.AreEqual(5, previewAssets.Length);
            Assert.AreEqual(1, previewAssets.OfType<Shader>().Count());
            Assert.AreEqual(1, previewAssets.OfType<Material>().Count());
            Assert.AreEqual(3, previewAssets.OfType<ComputeShader>().Count());

            int maxFrame = 64;
            while (maxFrame-- > 0 && vfxComponent.aliveParticleCount == 0)
                yield return null;
            Assert.IsTrue(maxFrame > 0);

            window.Focus();
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            for (int i = 0; i < 8; ++i)
                yield return null;

            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(m_Domain_Reload_With_VFX_Live_In_Scene_Asset));

            previewAssets = VFXTestCommon.GetPreviewAssets(m_Domain_Reload_With_VFX_Live_In_Scene_Graph);
            Assert.AreEqual(5, previewAssets.Length);
            Assert.AreEqual(1, previewAssets.OfType<Shader>().Count());
            Assert.AreEqual(1, previewAssets.OfType<Material>().Count());
            Assert.AreEqual(3, previewAssets.OfType<ComputeShader>().Count());
        }

        [SerializeField] private string m_Domain_Reload_Open_Same_Window_Twice_Path;

        [UnityTest, Description("Cover UUM-113965")]
        public IEnumerator Domain_Reload_Open_Same_Window_Twice()
        {
            while (EditorWindow.HasOpenInstances<VFXViewWindow>())
                EditorWindow.GetWindow<VFXViewWindow>().Close();
            while (EditorWindow.HasOpenInstances<SceneView>())
                EditorWindow.GetWindow<SceneView>().Close();

            Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count);
            var graph = VFXTestCommon.MakeTemporarySubGraphBlock();

            m_Domain_Reload_Open_Same_Window_Twice_Path = AssetDatabase.GetAssetPath(graph);
            Assert.IsFalse(string.IsNullOrEmpty(m_Domain_Reload_Open_Same_Window_Twice_Path));

            var resource = graph.GetResource();
            var window = VFXViewWindow.GetWindow(resource, true, true);
            window.LoadResource(resource, null);

            var vfxDockArea = window.m_Parent as DockArea;
            Assert.IsNotNull(vfxDockArea);
            vfxDockArea.AddTab(SceneView.GetWindow(typeof(SceneView)));

            for (int i = 0; i < 4; ++i)
                yield return null;

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);

            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            resource = VisualEffectResource.GetResourceAtPath(m_Domain_Reload_Open_Same_Window_Twice_Path);
            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            yield return null;

            window = VFXViewWindow.GetWindow(resource, true, true);
            window.LoadResource(resource, null);

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            yield return null;
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
