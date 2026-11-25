using System;
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
    public class VFXWindowReloadTests
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

        [SerializeField] private VisualEffectAsset m_Domain_Reload_With_VFX_Live_In_Scene_Asset;
        [SerializeField] private VFXGraph m_Domain_Reload_With_VFX_Live_In_Scene_Graph;

        [UnityTest, Description("Cover UUM-112719")]
        public IEnumerator Domain_Reload_With_VFX_Live_In_Scene()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();

            var sceneView = SceneView.GetWindow(typeof(SceneView));
            sceneView.position = new Rect(0, 0, 800, 600);

            var vfxGraph = VFXTestCommon.CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/Simple_Loop.vfx");
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
            var window = VFXTestCommon.GetWindow(vfxGraph, true, true);
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
            var window = VFXTestCommon.GetWindow(resource, true, true);
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

            window = VFXTestCommon.GetWindow(resource, true, true);
            window.LoadResource(resource, null);

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            yield return null;
        }

        internal enum ReloadEvent { SaveOnDisk, DomainReload }
        internal enum ChangeValueInput { Operator, Block }
        internal enum ChangeValuePosition { Before, After, }

        [Serializable]
        public class Change_ValueSetup
        {
            [SerializeField] internal ReloadEvent m_Event;
            [SerializeField] internal ChangeValueInput m_Input;
            [SerializeField] internal ChangeValuePosition m_Position;

            internal Change_ValueSetup(ReloadEvent evt, ChangeValueInput input, ChangeValuePosition position)
            {
                m_Event = evt;
                m_Input = input;
                m_Position = position;
            }

            public override string ToString()
            {
                return $"{m_Event}_{m_Input}_{m_Position}";
            }

            internal void FuncChangeValue(VFXInlineOperator inlineInteger, Block.VFXSpawnerSetAttribute setAttribute, int value)
            {
                if (m_Input == ChangeValueInput.Operator)
                    inlineInteger.inputSlots[0].value = value;
                else
                    setAttribute.inputSlots[0].value = (float)value;
            }
        }

        static readonly Change_ValueSetup[] kSave_Asset_On_Disk_And_Change_ValueCases =
            new[]
            {
                new Change_ValueSetup(ReloadEvent.SaveOnDisk, ChangeValueInput.Block, ChangeValuePosition.After),
                new Change_ValueSetup(ReloadEvent.SaveOnDisk, ChangeValueInput.Block, ChangeValuePosition.Before),
                new Change_ValueSetup(ReloadEvent.SaveOnDisk, ChangeValueInput.Operator, ChangeValuePosition.After),
                new Change_ValueSetup(ReloadEvent.SaveOnDisk, ChangeValueInput.Operator, ChangeValuePosition.Before),
                //new Change_ValueSetup(ReloadEvent.DomainReload, ChangeValueInput.Block, ChangeValuePosition.After), //This test implies domain reload which is slow, only test the most complex configuration
                new Change_ValueSetup(ReloadEvent.DomainReload, ChangeValueInput.Operator, ChangeValuePosition.After),
            };

        [SerializeField] GameObject m_ChangeValue_Camera;
        [SerializeField] VisualEffectAsset m_ChangeValue_Asset;
        [SerializeField] VFXInlineOperator m_InlineInteger;
        [SerializeField] Block.VFXSpawnerSetAttribute m_SetAttributeAge;
        [SerializeField] int m_LastReadOfAge;
        [SerializeField] Change_ValueSetup m_Setup;
        [SerializeField] VisualEffect m_ChangeValue_VisualEffect;
        const int m_ChangeValue_ExpectedNewValue = 67;
        const int m_ChangeValue_ExpectedInitialValue = 23;

        void ChangeValueReceived(VFXOutputEventArgs evt)
        {
            var newAge = (int)evt.eventAttribute.GetFloat("age");
            if (m_LastReadOfAge != newAge)
            {
                m_LastReadOfAge = newAge;
                //Debug.Log("EventReceived: " + m_LastReadOfAge);
            }
        }

        [UnityTest]
        public IEnumerator Change_Value([ValueSource(nameof(kSave_Asset_On_Disk_And_Change_ValueCases))] Change_ValueSetup setup)
        {
            m_Setup = setup;
            while (EditorWindow.HasOpenInstances<SceneView>())
                EditorWindow.GetWindow<SceneView>().Close();
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            //Prepare
            var vfxGraph = VFXTestCommon.CreateGraph_And_System();
            var outputEvent = ScriptableObject.CreateInstance<VFXOutputEvent>();
            var eventName = "Save_Asset_On_Disk_And_Change_Value";
            outputEvent.SetSettingValue("eventName", eventName);
            var basicSpawner = vfxGraph.children.OfType<VFXBasicSpawner>().Single();
            vfxGraph.AddChild(outputEvent);
            outputEvent.LinkFrom(basicSpawner);

            var setAttributeSpawnCount = ScriptableObject.CreateInstance<Block.VFXSpawnerSetAttribute>();
            setAttributeSpawnCount.SetSettingValue("attribute", "spawnCount");
            setAttributeSpawnCount.inputSlots[0].value = 1.0f;
            basicSpawner.AddChild(setAttributeSpawnCount);

            m_SetAttributeAge = ScriptableObject.CreateInstance<Block.VFXSpawnerSetAttribute>();
            m_SetAttributeAge.SetSettingValue("attribute", "age");
            basicSpawner.AddChild(m_SetAttributeAge);

            m_InlineInteger = ScriptableObject.CreateInstance<VFXInlineOperator>();
            m_InlineInteger.SetSettingValue("m_Type", (SerializableType)typeof(int));
            vfxGraph.AddChild(m_InlineInteger);
            if (m_Setup.m_Input == ChangeValueInput.Operator)
                Assert.IsTrue(m_SetAttributeAge.inputSlots[0].Link(m_InlineInteger.outputSlots[0]));

            m_Setup.FuncChangeValue(m_InlineInteger, m_SetAttributeAge, m_ChangeValue_ExpectedInitialValue);

            VFXTestCommon.ReimportVFXGraph(vfxGraph);
            yield return null;

            var mainObject = new GameObject("VFX_Test_Main_Object_" + setup);
            m_ChangeValue_Camera = new GameObject("VFX_Test_Main_Camera_" + setup);

            var camera = m_ChangeValue_Camera.AddComponent<Camera>();
            m_ChangeValue_Camera.tag = "MainCamera";
            camera.transform.localPosition = Vector3.one;
            camera.transform.LookAt(mainObject.transform.position);
            m_ChangeValue_VisualEffect = mainObject.AddComponent<VisualEffect>();
            m_ChangeValue_Asset = vfxGraph.GetResource().asset;
            m_ChangeValue_VisualEffect.visualEffectAsset = m_ChangeValue_Asset;

            m_LastReadOfAge = -1;
            m_ChangeValue_VisualEffect.outputEventReceived += ChangeValueReceived;
            yield return null;

            int maxFrame = 8;
            while (maxFrame-- > 0)
            {
                if (m_LastReadOfAge == m_ChangeValue_ExpectedInitialValue)
                    break;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            Assert.AreEqual(VFXCompilationMode.Runtime, VisualEffectAssetUtility.GetCompilationMode(m_ChangeValue_Asset));
            var window = VFXTestCommon.GetWindow(vfxGraph.GetResource(), true, true);
            Assert.IsNotNull(window);
            window.LoadResource(vfxGraph.GetResource(), null);
            maxFrame = 8;
            while (maxFrame-- > 0)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(m_ChangeValue_Asset) == VFXCompilationMode.Edition)
                    break;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            vfxGraph.children.OfType<VFXBasicUpdate>().Single().position = new Vector2(5, 6);
            Assert.IsTrue(EditorUtility.IsDirty(vfxGraph));

            //Actual repro here ⏬
            if (m_Setup.m_Position == ChangeValuePosition.Before)
            {
                m_Setup.FuncChangeValue(m_InlineInteger, m_SetAttributeAge, m_ChangeValue_ExpectedNewValue);
                yield return null;
            }

            if (m_Setup.m_Event == ReloadEvent.SaveOnDisk)
            {
                window.SaveChanges();
            }
            else
            {
                EditorUtility.RequestScriptReload();
                yield return new WaitForDomainReload();
                m_ChangeValue_VisualEffect.outputEventReceived += ChangeValueReceived;
            }

            maxFrame = 8;
            while (maxFrame-- > 0)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(m_ChangeValue_Asset) == VFXCompilationMode.Edition)
                    break;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);

            if (m_Setup.m_Position == ChangeValuePosition.After)
            {
                m_Setup.FuncChangeValue(m_InlineInteger, m_SetAttributeAge, m_ChangeValue_ExpectedNewValue);
                yield return null;
            }

            //Debug.Log("Await for event ⌛");
            maxFrame = 8;
            m_LastReadOfAge = -1;
            while (maxFrame-- > 0)
            {
                if (m_LastReadOfAge == m_ChangeValue_ExpectedNewValue)
                    break;
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0, "Failure of value modification");

            while (EditorWindow.HasOpenInstances<VFXViewWindow>())
            {
                var windows = VFXTestCommon.GetViewWindow();
                VFXTestCommon.MockingVFXViewWindows(windows);
                VFXTestCommon.GetViewWindow().Close();
                yield return null;
            }

            UnityEngine.Object.DestroyImmediate(m_ChangeValue_VisualEffect.gameObject);
            UnityEngine.Object.DestroyImmediate(m_ChangeValue_Camera);
            yield return null;
        }
    }
}
