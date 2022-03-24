using System;
using System.Text;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Generation;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphDelta.Utils;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(NewShaderGraphImporter))]
    class NewShaderGraphImporterEditor : ScriptedImporterEditor
    {
        MaterialEditor materialEditor = null;
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("View Generated Shader"))
            {
                AssetImporter importer = target as AssetImporter;
                var asset = (ShaderGraphAsset)AssetDatabase.LoadAssetAtPath(importer.assetPath, typeof(ShaderGraphAsset));
                var graph = asset.ResolveGraph();


                var reg = ShaderGraphRegistryBuilder.CreateDefaultRegistry();
                var key = Registry.ResolveKey<ShaderGraphContext>();
                var node = graph.GetNodeReader(key.Name);
                var shaderCode = Interpreter.GetShaderForNode(node, graph, reg);
                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                string path = $"Temp/GeneratedFromGraph-{assetName.Replace(" ", "")}.shader";
                if (FileHelpers.WriteToFile(path, shaderCode))
                    FileHelpers.OpenFile(path);
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
            var assetModel = ShaderGraphAsset.HandleLoad(path);
            return ShowWindow(path, assetModel);
        }

        private static bool ShowWindow(string path, ShaderGraphAssetModel model)
        {
            // Prevents the same graph asset from being opened in two separate editor windows
            var existingEditorWindows = (ShaderGraphEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(ShaderGraphEditorWindow));
            foreach (var existingEditorWindow in existingEditorWindows)
            {
                if (UnityEngine.Object.ReferenceEquals(existingEditorWindow.GraphTool.ToolState.AssetModel, model))
                    return true;
            }

            var shaderGraphEditorWindow = EditorWindow.CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
            if(shaderGraphEditorWindow == null)
            {
                return false;
            }
            shaderGraphEditorWindow.Show();
            shaderGraphEditorWindow.Focus();
            shaderGraphEditorWindow.SetCurrentSelection(model, GraphViewEditorWindow.OpenMode.OpenAndFocus);
            return true;
        }
    }
}
