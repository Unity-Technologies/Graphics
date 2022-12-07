using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphDelta.Utils;
using UnityEditor.ShaderGraph.GraphUI;
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
                var asset = (ShaderGraphAsset)AssetDatabase.LoadAssetAtPath(importer.assetPath, typeof(ShaderGraphAsset));
                var graph = asset.ShaderGraphModel.GraphHandler;

                var key = Registry.ResolveKey<Defs.ShaderGraphContext>();
                var node = graph.GetNode(key.Name);
                var shaderCode = Interpreter.GetShaderForNode(node, graph, graph.registry, out _, asset.ShaderGraphModel.ActiveTarget, asset.ShaderGraphModel.ShaderName);
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                string path = $"Temp/GeneratedFromGraph-{assetName.Replace(" ", "")}.shader";
                if (FileHelpers.WriteToFile(path, shaderCode))
                    FileHelpers.OpenFile(path);
            }

            if(GUILayout.Button("Save out generated Shader"))
            {
                AssetImporter importer = target as AssetImporter;
                var asset = (ShaderGraphAsset)AssetDatabase.LoadAssetAtPath(importer.assetPath, typeof(ShaderGraphAsset));
                var graph = asset.ShaderGraphModel.GraphHandler;

                var key = Registry.ResolveKey<Defs.ShaderGraphContext>();
                var node = graph.GetNode(key.Name);
                var shaderCode = Interpreter.GetShaderForNode(node, graph, graph.registry, out _, asset.ShaderGraphModel.ActiveTarget, asset.ShaderGraphModel.ShaderName);
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
            string path = AssetDatabase.GetAssetPath(instanceID);
            var graphAsset = ShaderGraphAssetUtils.HandleLoad(path);;
            if (graphAsset == null)
                return false;
            return graphAsset && ShowWindow(graphAsset);
        }

        static bool ShowWindow(ShaderGraphAsset asset)
        {
            var window = GraphViewEditorWindow.ShowGraphInExistingOrNewWindow<ShaderGraphEditorWindow>(asset);
            return window != null;
        }
    }
}
