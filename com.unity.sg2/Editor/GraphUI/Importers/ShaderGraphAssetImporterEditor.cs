using System;
using System.IO;
using System.Linq;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphDelta.Utils;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(ShaderGraphAssetImpoter))]
    class ShaderGraphAssetImporterEditor : ScriptedImporterEditor
    {
        MaterialEditor materialEditor = null;
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("View Generated Shader"))
            {
                AssetImporter importer = target as AssetImporter;
                var asset = InternalEditorUtility.LoadSerializedFileAndForget(importer.assetPath).OfType<ShaderGraphAsset>().FirstOrDefault();
                var graph = asset.SGGraphModel.GraphHandler;

                var key = Registry.ResolveKey<Defs.ShaderGraphContext>();
                var node = graph.GetNode(key.Name);
                var shaderCode = Interpreter.GetShaderForNode(node, graph, graph.registry, out _, asset.SGGraphModel.ActiveTarget, asset.SGGraphModel.ShaderName);
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                string path = $"Temp/GeneratedFromGraph-{assetName.Replace(" ", "")}.shader";
                if (FileHelpers.WriteToFile(path, shaderCode))
                    FileHelpers.OpenFile(path);
            }

            if(GUILayout.Button("Save out generated Shader"))
            {
                AssetImporter importer = target as AssetImporter;
                var asset = InternalEditorUtility.LoadSerializedFileAndForget(importer.assetPath).OfType<ShaderGraphAsset>().FirstOrDefault();
                var graph = asset.SGGraphModel.GraphHandler;

                var key = Registry.ResolveKey<Defs.ShaderGraphContext>();
                var node = graph.GetNode(key.Name);
                var shaderCode = Interpreter.GetShaderForNode(node, graph, graph.registry, out _, asset.SGGraphModel.ActiveTarget, asset.SGGraphModel.ShaderName);
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                File.WriteAllText($"Assets/{assetName}-GeneratedShader.shader", shaderCode);
                AssetDatabase.Refresh();
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath($"Assets/{assetName}-GeneratedShader.shader");
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            ApplyRevertGUI();
            if (materialEditor != null)
            {
                EditorGUILayout.Space();
                materialEditor.DrawHeader();
                using (new EditorGUI.DisabledGroupScope(true))
                    materialEditor.OnInspectorGUI();
            }
        }
        public override void OnEnable()
        {
            base.OnEnable();
            AssetImporter importer = target as AssetImporter;
            var material = AssetDatabase.LoadAssetAtPath<Material>(importer.assetPath);
            if (material)
                materialEditor = (MaterialEditor)CreateEditor(material);
        }
        public override void OnDisable()
        {
            base.OnDisable();
            if (materialEditor != null)
                DestroyImmediate(materialEditor);
        }

        [OnOpenAsset(0)]
        public static bool OnOpenShaderGraph(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            //var graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(path);
            var graphAsset = InternalEditorUtility.LoadSerializedFileAndForget(path).OfType<ShaderGraphAsset>().FirstOrDefault();
            if(graphAsset != null)
            {
                graphAsset.SetFilePath(path);
                return ShowWindow(path, graphAsset);
            }

            return false;
        }

        private static bool ShowWindow(string path, ShaderGraphAsset model)
        {
            var window = GraphViewEditorWindow.ShowGraphInExistingOrNewWindow<ShaderGraphEditorWindow>(model);
            return window != null;
        }
    }
}
