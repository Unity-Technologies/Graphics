using System;
using UnityEngine;
using UnityEditor;
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
using UnityEditor.ShaderGraph.Internal;
#if VFX_HAS_TIMELINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif

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

        public static VFXGraph CopyTemporaryGraph(string path)
        {
            var guid = System.Guid.NewGuid().ToString();
            string tempFilePath = string.Format(tempFileFormat, guid);
            System.IO.Directory.CreateDirectory(tempBasePath);
            File.Copy(path, tempFilePath);

            AssetDatabase.ImportAsset(tempFilePath);
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(tempFilePath);
            VisualEffectResource resource = asset.GetResource();
            var graph = resource.GetOrCreateGraph();
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
            var window = EditorWindow.GetWindow<VFXViewWindow>();
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
            VisualEffectResource resource = asset.GetOrCreateResource(); // force resource creation
            VFXGraph graph = resource.GetOrCreateGraph();
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

        public static VFXGraph CreateGraph_And_System()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

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
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graph));

            return graph;
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
    }
}
