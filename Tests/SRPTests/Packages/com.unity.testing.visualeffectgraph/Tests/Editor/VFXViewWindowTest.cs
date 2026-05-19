using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Moq;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXViewWindowTest
    {
        public bool m_GenerateShadersWithDebugSymbols;

        [OneTimeSetUp]
        public void Setup()
        {
            m_GenerateShadersWithDebugSymbols = VFXViewPreference.generateShadersWithDebugSymbols;

            EditorPrefs.SetBool(VFXViewPreference.generateShadersWithDebugSymbolsKey, false);
            VFXViewPreference.SetDirty();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            VFXTestCommon.DeleteAllTemporaryGraph();

            EditorPrefs.SetBool(VFXViewPreference.generateShadersWithDebugSymbolsKey, m_GenerateShadersWithDebugSymbols);
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

        [UnityTest]
        public IEnumerator Check_Tab_Attachment_Behavior()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
            while (EditorWindow.HasOpenInstances<VFXViewWindow>())
                VFXTestCommon.GetViewWindow().Close();

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

            var emptyVFX = VFXTestCommon.GetWindow((VFXGraph)null, true, true);
            yield return null;
            Assert.AreNotEqual(null, emptyVFX);

            Assert.AreEqual(emptyVFX.m_Parent, sceneView.m_Parent);
            Assert.AreEqual(2, dockArea.m_Panes.Count);
            Assert.IsTrue(emptyVFX.hasFocus);

            var dummyVFX = VFXTestCommon.MakeTemporaryGraph();
            var actualVFX = VFXTestCommon.GetWindow(dummyVFX, true, true);
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

        static string[] k_Check_Skip_Import_While_Opening_Windows_Scenario = {"Import", "Save"};

        [UnityTest]
        public IEnumerator Check_Skip_Import_While_Opening_Windows([ValueSource(nameof(k_Check_Skip_Import_While_Opening_Windows_Scenario))] string scenario)
        {
            var graph = VFXTestCommon.CreateGraph_And_System();

            //We are using SG Output to detect relevant change among asset listing while switch editor/runtime
            var sgOutput = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
            sgOutput.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
            var updateContext = graph.children.OfType<VFXBasicUpdate>().Single();
            updateContext.LinkTo(sgOutput);
            graph.AddChild(sgOutput);
            Assert.IsTrue(EditorUtility.IsDirty(graph));

            var path = AssetDatabase.GetAssetPath(graph);
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            var window = VFXTestCommon.GetViewWindow();

            //Insure Closing is saving to cover runtime mode on save
            var defaultMockView = new Mock<IVFXViewEditorAssetEventHandler>();
            defaultMockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Save);
            window.graphView.AssetEventHandler = defaultMockView.Object;

            Assert.AreEqual(VFXCompilationMode.Runtime, VisualEffectAssetUtility.GetCompilationMode(asset));
            Assert.AreEqual(1, AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Count()); //New SG Output isn't listed

            window.LoadResource(graph.GetResource(), null);
            for (int i = 0; i < 4; ++i)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(asset) == VFXCompilationMode.Edition)
                    break;
                yield return null;
            }
            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(asset));

            if (scenario == "Import")
            {
                AssetDatabase.ImportAsset(path);
                Assert.IsTrue(EditorUtility.IsDirty(graph));
            }
            else
            {
                window.SaveChanges();
                Assert.IsFalse(EditorUtility.IsDirty(graph));
            }

            for (int i = 0; i < 4; ++i)
            {
                Assert.AreEqual(3, AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Count()); //New SG Output is listed in editor mode (with variant)
                Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(asset));
                yield return null;
            }

            window.Close();
            Assert.IsFalse(EditorUtility.IsDirty(graph));
            for (int i = 0; i < 4; ++i)
            {
                Assert.AreEqual(VFXCompilationMode.Runtime, VisualEffectAssetUtility.GetCompilationMode(asset));
                Assert.AreEqual(2, AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Count()); //New SG Output is listed in runtime mode (no variant)
                yield return null;
            }
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


        [UnityTest]
        public IEnumerator Subgraph_Update_With_Main_Window_Opened()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_Subgraph_Add_Exposed_Input.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);

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

        [UnityTest]
        public IEnumerator Open_Graph_Delete_It_Expect_No_Asset()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var resource = graph.visualEffectResource;
            var window = VFXTestCommon.GetViewWindow();

            var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
            mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Discard);
            window.graphView.AssetEventHandler = mockView.Object;
            window.LoadResource(resource, null);

            for (int i = 0; i < 8; ++i)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset) == VFXCompilationMode.Edition)
                    break;
                yield return null;
            }
            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset));

            Assert.IsFalse(EditorUtility.IsDirty(graph));
            Assert.IsFalse(window.hasUnsavedChanges);
            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            initialize.position += Vector2.one;
            yield return null;

            Assert.IsTrue(EditorUtility.IsDirty(graph));
            Assert.IsTrue(window.hasUnsavedChanges);
            Assert.AreNotEqual("No Asset", window.titleContent.text);

            var vfxPath = AssetDatabase.GetAssetPath(graph);
            Assert.IsFalse(string.IsNullOrEmpty(vfxPath));
            AssetDatabase.DeleteAsset(vfxPath);
            yield return null;

            Assert.IsNotNull(graph);
            Assert.IsNotNull(resource);
            Assert.IsTrue(graph == null);
            Assert.IsTrue(resource == null);
            Assert.IsFalse(window.hasUnsavedChanges);
            Assert.AreEqual("No Asset", window.titleContent.text);

            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Never);
            window.Close();
            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Never);
            yield return null;
        }

        static bool[] kRepro_Modify_Save_Modify_With_Window_OpenedCase = { false, true };
        [UnityTest, Description("Cover WriteAssetWithSubAssets which can trigger an reimport switch asset from editor to runtime")]
        public IEnumerator Modify_Save_Modify_With_Window_Opened([ValueSource(nameof(kRepro_Modify_Save_Modify_With_Window_OpenedCase))] bool useReimport)
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.visualEffectResource, null);
            yield return null;

            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            var setAttribute = initialize.children.Single();

            var currentValue = setAttribute.inputSlots[0].value;
            Assert.IsInstanceOf<Position>(currentValue);

            var currentPosition = (Position)currentValue;
            currentPosition.position += Vector3.forward;

            Assert.IsFalse(EditorUtility.IsDirty(graph));
            setAttribute.inputSlots[0].value = currentPosition;
            Assert.IsTrue(EditorUtility.IsDirty(graph));

            if (useReimport)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));
                Assert.IsTrue(EditorUtility.IsDirty(graph));
            }
            else
            {
                //Explicit Save Scenario (which also triggers a reimport)
                window.graphView.OnSave();
                Assert.IsFalse(EditorUtility.IsDirty(graph));
            }

            currentPosition.position += Vector3.forward;
            setAttribute.inputSlots[0].value = currentPosition;

            for (int i = 0; i < 4; ++i)
                yield return null;

            window.Close();
            yield return null;
        }

        static object[] kAskSaveBehaviors =
            Enum.GetValues(typeof(AskAssetChangedBeforeCloseChoice))
                .OfType<object>().ToArray();

        static bool FindExpectedCapacityOnDisk(VFXGraph graph, uint expectedCapacity)
        {
            var expectedStringOnDisk = $"capacity: {expectedCapacity}";
            var assetPath = AssetDatabase.GetAssetPath(graph);
            var fileContent = File.ReadAllText(assetPath);
            var matchCount = Regex.Matches(fileContent, expectedStringOnDisk).Count;
            return matchCount == 1;
        }

        static bool FindExpectedCapacityOnMemory(VFXGraph graph, uint expectedCapacity)
        {
            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            var readCapacity = (uint)initialize.GetSettingValue("capacity");
            return readCapacity == expectedCapacity;
        }

        [UnityTest, Description("Cover Nodal Window behavior asking for save")]
        public IEnumerator Modify_Graph_And_Check_Nodal_Ask_Asset_Changed([ValueSource(nameof(kAskSaveBehaviors))] object askAssetChangedBeforeCloseChoiceObject)
        {
            var choice = (AskAssetChangedBeforeCloseChoice)askAssetChangedBeforeCloseChoiceObject;

            var graph = VFXTestCommon.CreateGraph_And_System();
            var resource = graph.GetResource();
            var vfxPath = AssetDatabase.GetAssetPath(resource);
            Assert.IsFalse(FindExpectedCapacityOnDisk(graph, 349u));
            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            initialize.SetSettingValue("capacity", 349u);
            VFXTestCommon.ReimportVFXGraph(graph);
            Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 349u));

            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.visualEffectResource, null);
            yield return null;

            var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
            mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(choice);
            window.graphView.AssetEventHandler = mockView.Object;

            Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 349u));

            Assert.IsFalse(EditorUtility.IsDirty(graph));
            initialize.SetSettingValue("capacity", 431u);
            Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 431u));
            Assert.IsTrue(EditorUtility.IsDirty(graph));
            yield return null;

            var currentCompilationMode = VisualEffectAssetUtility.GetCompilationMode(graph.GetResource().asset);
            Assert.AreEqual(VFXCompilationMode.Edition, currentCompilationMode);

            window.Close();
            yield return null;
            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Once);
            resource = VisualEffectResource.GetResourceAtPath(vfxPath);
            graph = resource.GetGraph();

            currentCompilationMode = VisualEffectAssetUtility.GetCompilationMode(resource.asset);
            if (choice == AskAssetChangedBeforeCloseChoice.Save)
            {
                Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 431u));
                Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 431u));
                Assert.IsFalse(EditorUtility.IsDirty(graph));
                Assert.AreEqual(VFXCompilationMode.Runtime, currentCompilationMode);
                Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 0);
            }
            else if (choice == AskAssetChangedBeforeCloseChoice.Cancel)
            {
                Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 349u));
                Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 431u));
                Assert.IsTrue(EditorUtility.IsDirty(graph));
                Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 1);
                Assert.AreEqual(VFXCompilationMode.Edition, currentCompilationMode);
                Assert.AreEqual(graph, VFXViewWindow.GetAllWindows().First().graphView.controller.graph);
            }
            else if (choice == AskAssetChangedBeforeCloseChoice.Discard)
            {
                Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 349u));
                Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 349u));
                Assert.IsFalse(EditorUtility.IsDirty(graph));
                Assert.AreEqual(VFXCompilationMode.Runtime, currentCompilationMode);
                Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 0);
            }
            else if (choice == AskAssetChangedBeforeCloseChoice.Ignore)
            {
                //Same than cancel but view is closed, this is only valid in test
                Assert.IsTrue(FindExpectedCapacityOnDisk(graph, 349u));
                Assert.IsTrue(FindExpectedCapacityOnMemory(graph, 431u));
                Assert.IsTrue(EditorUtility.IsDirty(graph));
                Assert.AreEqual(VFXCompilationMode.Edition, currentCompilationMode);
                Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 0);
            }
            else
            {
                Assert.Fail("Unexpected choice: " + choice);
            }

            //Try reopen, check dirty status and close
            if (VFXViewWindow.GetAllWindows().Count == 0)
            {
                window = VFXTestCommon.GetViewWindow();
                window.LoadResource(graph.visualEffectResource, null);
                yield return null;

                if (choice != AskAssetChangedBeforeCloseChoice.Ignore)
                {
                    Assert.IsFalse(EditorUtility.IsDirty(graph));
                }
                else
                {
                    Assert.IsTrue(EditorUtility.IsDirty(graph));
                }

                window.graphView.AssetEventHandler = mockView.Object;
                window.Close();
                yield return null;

                if (choice != AskAssetChangedBeforeCloseChoice.Ignore)
                {
                    mockView.VerifyNoOtherCalls();
                }
                else
                {
                    mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Exactly(2));
                }
            }
            VFXTestCommon.CloseAllVFXWindow();
        }

        [UnityTest, Description("Cover Nodal Window revert, insure that cache value are invalidated")]
        public IEnumerator Modify_Graph_And_Check_On_Enable_Invoked()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var vfxPath = AssetDatabase.GetAssetPath(graph);
            var resource = graph.visualEffectResource;
            var initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            var setAttribute = initialize.children.Single();

            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(graph.visualEffectResource, null);
            yield return null;
            Assert.IsTrue(setAttribute.enabled);

            Assert.IsFalse(EditorUtility.IsDirty(graph));
            setAttribute.activationSlot.value = false;
            Assert.IsTrue(EditorUtility.IsDirty(graph));
            yield return null;

            Assert.IsFalse(setAttribute.enabled);
            var currentCompilationMode = VisualEffectAssetUtility.GetCompilationMode(resource.asset);
            Assert.AreEqual(VFXCompilationMode.Edition, currentCompilationMode);

            var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
            mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Discard);
            window.graphView.AssetEventHandler = mockView.Object;

            window.Close();
            yield return null;
            mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Once);

            //Former SetAttribute block is expected to be deleted by previous ReloadFromDisk
            Assert.IsTrue(setAttribute == null);
            Assert.IsFalse(System.Object.ReferenceEquals(setAttribute, null));
            resource = VisualEffectResource.GetResourceAtPath(vfxPath);
            graph = resource.GetGraph();
            initialize = graph.children.OfType<VFXBasicInitialize>().Single();
            setAttribute = initialize.children.Single();

            currentCompilationMode = VisualEffectAssetUtility.GetCompilationMode(resource.asset);
            Assert.IsFalse(EditorUtility.IsDirty(resource));
            Assert.AreEqual(VFXCompilationMode.Runtime, currentCompilationMode);
            Assert.AreEqual(VFXViewWindow.GetAllWindows().Count, 0);
            Assert.IsTrue(setAttribute.enabled, "SetAttribute is probably still caching former enable state.");

            yield return null;
        }

        static readonly string[] kOpen_Graph_With_Missing_Reference_And_DiscardCaseSource =
        {
            "Repro_Missing_Reference_HDRP.vfx_",
            "Repro_Missing_Reference_URP.vfx_",
            "Repro_Old_Camera_Reference.vfx_"
        };

        static readonly bool[] kOpen_Graph_With_Missing_Reference_And_DiscardCaseModify =
        {
            false,
            //Cover an issue where modifying activation slot won't be restored in its initial state while loading Repro_Old_Camera_Reference
            //m_ActivationSlot is kept after ReloadFromDisk, m_ActivationSlot being a new field between 22.3 & current version.
            true
        };

        [UnityTest, Description("Cover revert on disk with missing data")]
        public IEnumerator Open_Graph_With_Missing_Reference_And_Discard(
            [ValueSource(nameof(kOpen_Graph_With_Missing_Reference_And_DiscardCaseSource))] string sourceFile,
            [ValueSource(nameof(kOpen_Graph_With_Missing_Reference_And_DiscardCaseModify))] bool modifyActivationSlot)
        {
            var sourcePath = Path.Combine("Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/", sourceFile);
            var originalContent = File.ReadAllText(sourcePath);

            var vfxGraph = VFXTestCommon.CopyTemporaryGraph(sourcePath);
            var vfxPath = AssetDatabase.GetAssetPath(vfxGraph);
            var vfxAsset = vfxGraph.GetResource().asset;
            Assert.IsNotNull(vfxAsset);
            Assert.IsNotNull(vfxPath);
            Assert.AreEqual(originalContent, File.ReadAllText(vfxPath));

            VFXTestCommon.CloseAllVFXWindow();

            for (int i = 0; i < 2; i++)
            {
                var previousContent = File.ReadAllText(vfxPath);

                vfxGraph = vfxAsset.GetResource().GetGraph();
                Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count());
                Assert.IsFalse(EditorUtility.IsDirty(vfxGraph));

                var window = VFXTestCommon.GetWindow(vfxGraph, true, true);
                window.LoadResource(vfxGraph.visualEffectResource, null);
                yield return null;

                bool isThereASanitize = EditorUtility.IsDirty(vfxGraph);
                if (sourceFile.StartsWith("Repro_Missing_Reference"))
                {
                    //N.B.: This following test is only valid until we are changing something in models used in Repro_Missing_Reference
                    Assert.IsFalse(isThereASanitize);
                }

                if (modifyActivationSlot)
                {
                    var basicUpdate = vfxGraph.children.OfType<VFXBasicUpdate>().First(o => o.GetNbChildren() == 1);
                    var block = basicUpdate.children.First();
                    Assert.IsTrue((bool)block.activationSlot.value, "Revert failure at iteration: " + i);
                    block.activationSlot.value = false;
                }

                vfxGraph.children.First().position += Vector2.one;
                Assert.IsTrue(EditorUtility.IsDirty(vfxGraph));
                Assert.AreEqual(previousContent, File.ReadAllText(vfxPath));

                var mockView = new Mock<IVFXViewEditorAssetEventHandler>();
                mockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Discard);
                window.graphView.AssetEventHandler = mockView.Object;

                window.Close();
                yield return null;
                mockView.Verify(x => x.AskAssetChangedBeforeClose(It.IsAny<string>()), Times.Once);

                var resource = VisualEffectResource.GetResourceAtPath(vfxPath);
                vfxGraph = resource.GetGraph();
                Assert.IsFalse(EditorUtility.IsDirty(vfxGraph));
                Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count());

                var finalContent = File.ReadAllText(vfxPath);
                Assert.AreEqual(previousContent, finalContent);
            }
        }

        [UnityTest, Description("Repro expected behavior from importing same package twice while having the window opened")]
        public IEnumerator Open_Graph_And_Delete_And_Restore()
        {
            var packagePath = Path.Combine("Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/", "Repro_Delete_And_Reimport.unitypackage");
            AssetDatabase.ImportPackageImmediately(packagePath);

            var vfxPath = Path.Combine(VFXTestCommon.tempBasePath, "Delete_And_Reimport.vfx");
            var subgraphPath = Path.Combine(VFXTestCommon.tempBasePath, "Delete_And_Reimport.vfxoperator");

            VFXTestCommon.CloseAllVFXWindow();
            for (int i = 0; i < 3; i++)
            {
                AssetDatabase.ImportPackageImmediately(packagePath);
                var vfxResource = VisualEffectResource.GetResourceAtPath(vfxPath);
                var subgraphResource = VisualEffectResource.GetResourceAtPath(subgraphPath);
                Assert.IsNotNull(vfxResource);
                Assert.IsNotNull(subgraphResource);
                Assert.AreNotEqual(vfxResource, subgraphResource);

                var windowSubGraph = VFXTestCommon.GetWindow(subgraphResource, true, true);
                windowSubGraph.LoadResource(subgraphResource, null);
                yield return null;

                var windowMain = VFXTestCommon.GetWindow(vfxResource, true, true);
                windowMain.LoadResource(vfxResource, null);
                Assert.AreEqual(2, VFXViewWindow.GetAllWindows().Count());

                for (int frame = 0; frame < 4; ++frame)
                    yield return null;
                Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(vfxResource.asset));

                var view = windowMain.graphView;
                var controller = windowMain.graphView.controller;
                Assert.IsNotNull(view.AssetEventHandler);
                Assert.AreEqual(1, controller.useCount);

                VFXTestCommon.DeleteAllTemporaryGraph();
                yield return null;

                Assert.AreEqual(0, controller.useCount, "DisconnectController might not have been invoked.");
                Assert.IsNull(view.AssetEventHandler, "DisconnectController might not have been invoked.");
            }
        }

        [UnityTest]
        public IEnumerator Insure_Unregister_Authoring_Data_Is_Done_When_Closing()
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var resource = graph.GetResource();
            var path = AssetDatabase.GetAssetPath(resource);
            var guid = AssetDatabase.GUIDFromAssetPath(path);

            VFXTestCommon.CloseAllVFXWindow();
            var windowSubGraph = VFXTestCommon.GetWindow(resource, true, true);
            windowSubGraph.LoadResource(resource, null);
            for (int frame = 0; frame < 2; ++frame)
                yield return null;
            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(resource.asset));

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count());

            using (var customLogHandler = new CustomLogHandler())
            {
                customLogHandler.ExpectedLog(LogType.Error, "Already registered");
                VFXGraph.RegisterAuthoringCompileData(guid);
            }

            VFXTestCommon.CloseAllVFXWindow();
            yield return null;

            Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count());
            Assert.AreEqual(VFXCompilationMode.Runtime, VisualEffectAssetUtility.GetCompilationMode(resource.asset));

            using (var customLogHandler = new CustomLogHandler())
            {
                customLogHandler.ExpectedLog(LogType.Error, "unknown authoring guid");
                VFXGraph.UnregisterAuthoringCompileData(guid);
            }

            Assert.DoesNotThrow(() => VFXGraph.RegisterAuthoringCompileData(guid));
            Assert.DoesNotThrow(() => VFXGraph.UnregisterAuthoringCompileData(guid));
        }


        [UnityTest, Description("Corner case discovered while verifying #73468")]
        public IEnumerator ConvertToSubGraph_With_CustomHLSL()
        {
            AssetDatabase.ImportPackage("Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_Subgraph_CustomHLSL.unitypackage", false);
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

            maxFrame = 8;
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

            maxFrame = 8;
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
