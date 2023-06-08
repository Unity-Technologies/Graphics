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
using UnityEditor.ShaderGraph.Internal;
#if VFX_HAS_TIMELINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif

[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests-testable")]

namespace UnityEditor.VFX.Test
{
    class VFXTestCommon
    {
        public static readonly string simpleParticleSystemPath = "Packages/com.unity.testing.visualeffectgraph/CommonAssets/VFX/SimpleParticleSystem.vfx";
        public static readonly string tempBasePath = "Assets/TmpTests/";
        static readonly string tempFileFormat = tempBasePath + "vfx_{0}.vfx";
        static readonly string tempFileFormatPlayable = tempBasePath + "vfx_{0}.playable";

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

            var contextInitializeDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.Contains("Init"));
            var contextOutputDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name.StartsWith("Output Particle Quad"));
            for (int i = 0; i < count; ++i)
            {
                var output = viewController.AddVFXContext(new Vector2(2 * i, 2 * i), contextOutputDesc);
                var init = viewController.AddVFXContext(new Vector2(i, i), contextInitializeDesc);

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
