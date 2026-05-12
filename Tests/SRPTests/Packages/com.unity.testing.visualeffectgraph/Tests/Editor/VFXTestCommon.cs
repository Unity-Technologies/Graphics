using System;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.VFX;
using UnityEditor.VFX.UI;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;

#if VFX_HAS_TIMELINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif
using Object = UnityEngine.Object;

using Moq;

[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.PerformanceEditorTests")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.PerformanceEditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.Tests")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.Tests-testable")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests-testable")]

namespace UnityEditor.VFX.Test
{
    //Equivalent of LogAssert but always works during import
    //LogAssert.Expect(LogType.Error, new Regex("You must use an unlit vfx master node with an unlit output"));
    //LogAssert.Expect(LogType.Error, new Regex("System.InvalidOperationException"));
    //It also provides the ability of breaking on log while running test
    class CustomLogHandler : ILogHandler, IDisposable
    {
        private ILogHandler m_OriginalHandler;
        private Dictionary<Regex, Type> m_ExpectedException = new();
        private Dictionary<string, Type> m_ActualException = new();
        private Dictionary<Regex, LogType> m_ExpectedLogs = new();
        private Dictionary<string, LogType> m_ActualLogs = new();

        public CustomLogHandler()
        {
            m_OriginalHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = this;
        }

        public void Reset()
        {
            m_ExpectedException.Clear();
            m_ActualException.Clear();
            m_ExpectedLogs.Clear();
            m_ActualLogs.Clear();
        }

        public void Clear()
        {
            m_ActualException.Clear();
            m_ActualLogs.Clear();
        }

        public void ExpectedLog(LogType type, string message)
        {
            m_ExpectedLogs.Add(new Regex(message), type);
        }

        public void ExpectedException(Type type, string message)
        {
            m_ExpectedException.Add(new Regex(message), type);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            var message = string.Format(format, args);

            foreach (var expectedLog in m_ExpectedLogs)
            {
                if (expectedLog.Value == logType && expectedLog.Key.IsMatch(message))
                {
                    m_ActualLogs.TryAdd(message, expectedLog.Value);
                    return;
                }
            }

            m_OriginalHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            foreach (var expectedException in m_ExpectedException)
            {
                if (expectedException.Value == exception.GetType() && expectedException.Key.IsMatch(exception.Message))
                {
                    m_ActualException.TryAdd(exception.Message, expectedException.Value);
                    return;
                }
            }
            m_OriginalHandler.LogException(exception, context);
        }

        public void Dispose()
        {
            Assert.AreEqual(m_ExpectedLogs.Count, m_ActualLogs.Count, "Expected logs count do not match actual log count");
            Assert.AreEqual(m_ExpectedException.Count, m_ActualException.Count, "Expected exception count do not match actual exception count");
            Debug.unityLogger.logHandler = m_OriginalHandler;
            Reset();
        }
    }

    class VFXTestCommon
    {
        public static readonly string simpleParticleSystemPath = "Packages/com.unity.testing.visualeffectgraph/CommonAssets/VFX/SimpleParticleSystem.vfx";
        public static readonly string tempBasePath = "Assets/TmpTests/";
        static readonly string tempFileFormat = tempBasePath + "vfx_{0}.vfx";
        static readonly string tempFileFormatPlayable = tempBasePath + "vfx_{0}.playable";

        public static readonly VFXValueType[] s_supportedValueType =
        {
            VFXValueType.Float,
            VFXValueType.Float2,
            VFXValueType.Float3,
            VFXValueType.Float4,
            VFXValueType.Int32,
            VFXValueType.Uint32,
            VFXValueType.Curve,
            VFXValueType.ColorGradient,
            VFXValueType.Mesh,
            VFXValueType.Texture2D,
            VFXValueType.Texture2DArray,
            VFXValueType.Texture3D,
            VFXValueType.TextureCube,
            VFXValueType.TextureCubeArray,
            VFXValueType.Boolean,
            VFXValueType.Matrix4x4
        };

        [MenuItem("Tests/VFX/Create Many VFX and Open")]
        public static void CreateManyVFX_And_Open()
        {
            var resourceToOpen = new List<VisualEffectResource>();
            int createManyVFXAndOpen = 32;
            for (int i = 0; i < createManyVFXAndOpen; ++i)
            {
                var vfxGraph = CopyTemporaryGraph("Packages/com.unity.visualeffectgraph/Editor/Templates/02_Simple_Loop.vfx", $"{i + 1}_");
                var vfxResource = vfxGraph.GetResource();
                resourceToOpen.Add(vfxResource);
            }

            foreach (var vfxResource in resourceToOpen)
            {
                var vfxWindow = GetWindow(vfxResource, true, true);
                vfxWindow.LoadResource(vfxResource);
            }
        }

        [MenuItem("Tests/VFX/Modify All Opened Graph")]
        public static void ModifyAllOpenedGraph()
        {
            var allWindows = VFXViewWindow.GetAllWindows().ToList();
            Unity.Profiling.Editor.UI.EditorCoroutineUtility.StartCoroutineOwnerless(ModifyAllOpenedGraphFocus(allWindows));
        }

        private static IEnumerator ModifyAllOpenedGraphFocus(List<VFXViewWindow> allWindows)
        {
            foreach (var vfxWindow in allWindows)
            {
                vfxWindow.Focus();
                yield return new Unity.Profiling.Editor.UI.EditorWaitForSeconds(0.1f);

                var vfxContext = vfxWindow.graphView.controller.graph.children.OfType<VFXContext>().First();
                vfxContext.position += Vector2.one;
                vfxWindow.graphView.controller.LightApplyChanges();
                yield return new Unity.Profiling.Editor.UI.EditorWaitForSeconds(0.1f);
            }
        }

        public static void CloseAllUnecessaryWindows()
        {
            //See UUM-14622: AssetImport during inspector rendering is creating instabilities
            while (EditorWindow.HasOpenInstances<InspectorWindow>())
                EditorWindow.GetWindow<InspectorWindow>().Close(); // Panel:Repaint => Editor:IsAppropriateFileOpenForEdit => Destroying GameObjects immediately is not permitted
            while (EditorWindow.HasOpenInstances<ProjectBrowser>())
                EditorWindow.GetWindow<ProjectBrowser>().Close(); //ProjectBrowser:OnGUI => OnDidAddComponent from HDAdditionalLightData => Send Message is forbidden
        }

        //Emulate function because VisualEffectUtility.GetSpawnerState has been removed
        //Prefer usage of GetSpawnSystemInfo for new implementation
        public static VFXSpawnerState GetSpawnerState(VisualEffect vfx, uint index)
        {
            var spawnerList = new List<string>();
            vfx.GetSystemNames(spawnerList);

            if (index >= spawnerList.Count)
                throw new IndexOutOfRangeException();

            return vfx.GetSpawnSystemInfo(spawnerList[(int)index]);
        }

        public static VFXGraph CopyTemporaryGraph(string path, string prefix = null)
        {
            var guid = System.Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(prefix))
                prefix = "vfx_";

            string tempFilePath = $"{tempBasePath}{prefix}{guid}.vfx";
            System.IO.Directory.CreateDirectory(tempBasePath);
            File.Copy(path, tempFilePath);

            AssetDatabase.ImportAsset(tempFilePath);
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(tempFilePath);
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetGraph();
            return graph;
        }

        public static ShaderGraphVfxAsset CopyTemporaryShaderGraph(string path)
        {
            var guid = System.Guid.NewGuid().ToString();
            var tempFilePath = $"{tempBasePath}sg_{guid}.shadergraph";
            System.IO.Directory.CreateDirectory(tempBasePath);
            File.Copy(path, tempFilePath);

            AssetDatabase.ImportAsset(tempFilePath);
            var shaderGraphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(tempFilePath);
            return shaderGraphAsset;
        }

#if VFX_HAS_TIMELINE
        public static TimelineAsset CopyTemporaryTimeline(string path)
        {
            var guid = System.Guid.NewGuid().ToString();
            string tempFilePath = string.Format(tempFileFormatPlayable, guid);
            System.IO.Directory.CreateDirectory(tempBasePath);
            File.Copy(path, tempFilePath);

            AssetDatabase.ImportAsset(tempFilePath);
            var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempFilePath);
            return asset;
        }
#endif

        public static VFXViewController StartEditTestAsset()
        {
            var window = VFXTestCommon.GetViewWindow();
            window.Show();
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var viewController = VFXViewController.GetController(graph.GetResource(), true);
            window.graphView.controller = viewController;
            return viewController;
        }

        public static VFXGraph MakeTemporaryGraph()
        {
            var guid = System.Guid.NewGuid().ToString();
            string tempFilePath = string.Format(tempFileFormat, guid);
            System.IO.Directory.CreateDirectory(tempBasePath);

            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(tempFilePath);
            VisualEffectResource resource = asset.GetResource();
            VFXGraph graph = resource.GetGraph();
            return graph;
        }

        public static VisualEffectSubgraphBlock MakeTemporarySubGraphBlock()
        {
            var guid = System.Guid.NewGuid().ToString();
            string tempFilePath = string.Format(tempFileFormat, guid).Replace("vfx", "vfxblock");
            System.IO.Directory.CreateDirectory(tempBasePath);

            VisualEffectAssetEditorUtility.CreateVisualEffectSubgraph<VisualEffectSubgraphBlock, VisualEffectAssetEditorUtility.DoCreateNewSubgraphBlock>(tempFilePath, VisualEffectAssetEditorUtility.templateBlockSubgraphAssetName);
            var projectBrowser = EditorWindow.GetWindow<ProjectBrowser>();
            projectBrowser.EndRenaming();

            AssetDatabase.ImportAsset(tempFilePath);
            return AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphBlock>(tempFilePath);
        }

        public static VisualEffectSubgraphOperator MakeTemporarySubGraphOperator()
        {
            var guid = System.Guid.NewGuid().ToString();
            string tempFilePath = string.Format(tempFileFormat, guid).Replace("vfx", "vfxoperator");
            System.IO.Directory.CreateDirectory(tempBasePath);

            VisualEffectAssetEditorUtility.CreateVisualEffectSubgraph<VisualEffectSubgraphOperator, VisualEffectAssetEditorUtility.DoCreateNewSubgraphOperator>(tempFilePath, VisualEffectAssetEditorUtility.templateOperatorSubgraphAssetName);
            var projectBrowser = EditorWindow.GetWindow<ProjectBrowser>();
            projectBrowser.EndRenaming();

            AssetDatabase.ImportAsset(tempFilePath);
            return AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphOperator>(tempFilePath);
        }

        public static void CreateSystem(VFXGraph graph)
        {
            var output = ScriptableObject.CreateInstance<VFXPointOutput>();
            output.SetSettingValue("castShadows", true);
            graph.AddChild(output);

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();

            var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
            var blockAttribute = blockAttributeDesc.variant.CreateInstance();
            blockAttribute.SetSettingValue("attribute", "position");
            contextInitialize.AddChild(blockAttribute);

            var contextUpdate = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            graph.AddChild(contextUpdate);
            contextInitialize.LinkTo(contextUpdate);

            contextUpdate.LinkTo(output);
            graph.AddChild(contextInitialize);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            spawner.LinkTo(contextInitialize);
            graph.AddChild(spawner);
        }

        public static VFXGraph CreateGraph_And_System()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            VFXTestCommon.CreateSystem(graph);
            VFXTestCommon.ReimportVFXGraph(graph);
            return graph;
        }

        public static void CloseAllVFXWindow()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(
                    x =>
                    {
                        MockingVFXViewWindowsIfNeeded(x);
                        x.Close();
                    });

            if (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                EditorWindow.GetWindow<GraphViewTemplateWindow>()?.Close();
            }
        }

        public static VFXViewWindow GetWindow(VFXGraph vfxGraph, bool createIfNeeded = false, bool show = true)
        {
            var currentWindow = VFXViewWindow.GetWindow(vfxGraph, createIfNeeded, show);
            MockingVFXViewWindowsIfNeeded(currentWindow);
            return currentWindow;
        }

        public static VFXViewWindow GetWindow(VisualEffectResource vfxResource, bool createIfNeeded = false, bool show = true)
        {
            var currentWindow = VFXViewWindow.GetWindow(vfxResource, createIfNeeded, show);
            MockingVFXViewWindowsIfNeeded(currentWindow);
            return currentWindow;
        }

        public static VFXViewWindow GetWindow(VisualEffectAsset vfxAsset, bool createIfNeeded = false)
        {
            var currentWindow = VFXViewWindow.GetWindow(vfxAsset, createIfNeeded);
            MockingVFXViewWindowsIfNeeded(currentWindow);
            return currentWindow;
        }

        public static VFXViewWindow GetViewWindow()
        {
            var currentWindow = VFXViewWindow.GetWindow<VFXViewWindow>();
            MockingVFXViewWindowsIfNeeded(currentWindow);
            return currentWindow;
        }

        private static void MockingVFXViewWindowsIfNeeded(VFXViewWindow currentWindow)
        {
            if (currentWindow.graphView != null &&
                (currentWindow.graphView.AssetEventHandler == null
                 || currentWindow.graphView.AssetEventHandler == currentWindow.graphView))
            {
                MockingVFXViewWindows(currentWindow);
            }
        }

        static Mock<IVFXViewEditorAssetEventHandler> s_DefaultMockView;
        public static void MockingVFXViewWindows(VFXViewWindow currentWindow, bool onSaveOnClose = true)
        {
            if (s_DefaultMockView == null)
            {
                s_DefaultMockView = new Mock<IVFXViewEditorAssetEventHandler>();
                s_DefaultMockView.Setup(x => x.AskAssetChangedBeforeClose(It.IsAny<string>())).Returns(AskAssetChangedBeforeCloseChoice.Ignore);
            }
            currentWindow.graphView.AssetEventHandler = s_DefaultMockView.Object;
        }

        public static void ReimportVFXGraph(VFXGraph graph)
        {
            graph.GetResource().WriteAsset();
        }

        public static void DeleteAllTemporaryGraph()
        {
            if (Directory.Exists(tempBasePath))
            {
                Directory.Delete(tempBasePath, true);
            }

            var meta = tempBasePath.Substring(0, tempBasePath.Length - 1) + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }
            AssetDatabase.Refresh();
        }

        public static IEnumerable<VFXExpression> CollectParentExpression(VFXExpression expression, HashSet<VFXExpression> hashSet = null)
        {
            if (expression != null)
            {
                if (hashSet == null)
                {
                    hashSet = new HashSet<VFXExpression>();
                }

                if (!hashSet.Contains(expression))
                {
                    hashSet.Add(expression);
                    yield return expression;
                    foreach (var parent in expression.parents)
                    {
                        var parents = CollectParentExpression(parent, hashSet);
                        foreach (var exp in parents)
                        {
                            yield return exp;
                        }
                    }
                }
            }
        }

        public static U GetFieldValue<T, U>(T obj, string fieldName)
            where U : class
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue(field != null, fieldName + ": field not found");
            return field.GetValue(obj) as U;
        }

        public static void SetFieldValue<T, U>(T obj, string fieldName, U value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue(field != null, fieldName + ": field not found");
            field.SetValue(obj, value);
        }

        public static void CallMethod<T>(T obj, string methodName, object[] parameters)
        {
            var methodInfo = obj.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue(methodInfo != null, methodName + ": method not found");
            methodInfo.Invoke(obj, new object[] { null });
        }

        internal static void SetTextFieldValue(VFXSystemBorder sys, string value)
        {
            var systemTextField = GetFieldValue<VFXSystemBorder, TextField>(sys, "m_TitleField");
            systemTextField.value = value;
            SetFieldValue(sys, "m_TitleField", systemTextField);
        }

        public static Object[] GetPreviewAssets(VFXGraph vfxGraph)
        {
            var previewAssetField = vfxGraph.GetType().GetField("m_PreviewAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(previewAssetField);
            var valuePreviewAsset = previewAssetField.GetValue(vfxGraph);
            Assert.IsNotNull(valuePreviewAsset);
            Assert.IsInstanceOf<List<Object>>(valuePreviewAsset);
            return ((List<Object>)valuePreviewAsset).ToArray();
        }

        internal static IEnumerable CheckCompilation(VFXGraph vfxGraph)
        {
            var resource = vfxGraph.GetResource();
            EditorUtility.SetDirty(resource);
            VFXTestCommon.ReimportVFXGraph(vfxGraph);
            var path = AssetDatabase.GetAssetPath(vfxGraph);

            for (int i = 0; i < 4; ++i)
                yield return null;

            while (ShaderUtil.anythingCompiling)
                yield return null;

            var computeShaders = AssetDatabase.LoadAllAssetsAtPath(path).OfType<ComputeShader>().ToArray();
            Assert.AreEqual(3, computeShaders.Length);

            foreach (var computeShader in computeShaders)
            {
                var messages = ShaderUtil.GetComputeShaderMessages(computeShader);
                foreach (var message in messages)
                    Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);

                Assert.AreEqual(0, computeShader.FindKernel("CSMain"));
                Assert.IsTrue(computeShader.IsSupported(0));
            }
            yield return null;
        }

        internal static void CreateSystems(VFXView view, VFXViewController viewController, int count, int offset, string name = null)
        {
            VFXContextController GetContextController(VFXContext context)
            {
                viewController.ApplyChanges();
                return viewController.allChildren.OfType<VFXContextController>().Single(x => x.model == context);
            }

            var contextInitializeDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.variant.name.Contains("Init"));
            var contextOutputDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.variant.name.StartsWith("Output Particle".AppendLabel("Unlit").AppendLabel("Quad")));
            for (int i = 0; i < count; ++i)
            {
                var output = viewController.AddVFXContext(new Vector2(2 * i, 2 * i), contextOutputDesc.variant);
                var init = viewController.AddVFXContext(new Vector2(i, i), contextInitializeDesc.variant);

                var flowEdge = new VFXFlowEdgeController(GetContextController(output).flowInputAnchors.FirstOrDefault(), GetContextController(init).flowOutputAnchors.FirstOrDefault());
                viewController.AddElement(flowEdge);
            }

            viewController.ApplyChanges();

            if (name != null)
            {
                var systems = GetFieldValue<VFXView, List<VFXSystemBorder>>(view, "m_Systems");
                foreach (var sys in systems)
                {
                    SetTextFieldValue(sys, name);
                    CallMethod(sys, "OnTitleBlur", new object[] { null });
                }
            }
        }

        internal static List<VFXBasicSpawner> CreateSpawners(VFXView view, VFXViewController viewController, int count, string name = null)
        {
            List<VFXBasicSpawner> spawners = new List<VFXBasicSpawner>();
            for (int i = 0; i != count; ++i)
            {
                var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                spawners.Add(spawner);
                viewController.graph.AddChild(spawner);
            }

            viewController.ApplyChanges();

            if (name != null)
            {
                var elements = view.Query().OfType<GraphElement>().ToList();
                var UIElts = elements.OfType<VFXContextUI>().ToList();
                var contextUITextField = GetFieldValue<VFXContextUI, TextField>(UIElts[0], "m_TextField");
                contextUITextField.value = name;

                foreach (var contextUI in UIElts)
                {
                    SetFieldValue(contextUI, "m_TextField", contextUITextField);
                    CallMethod(contextUI, "OnTitleBlur", new object[] { null });
                }
            }

            return spawners;
        }
        public static ShaderInclude CreateShaderFile(string hlslCode, out string destinationPath)
        {
            destinationPath = Path.Combine(VFXTestCommon.tempBasePath, Guid.NewGuid() + ".hlsl");
            Directory.CreateDirectory(VFXTestCommon.tempBasePath);
            File.WriteAllText(destinationPath, hlslCode);
            AssetDatabase.ImportAsset(destinationPath);
            var shaderInclude = (ShaderInclude)AssetDatabase.LoadAssetAtPath(destinationPath, typeof(ShaderInclude));
            return shaderInclude;
        }

    }
}
