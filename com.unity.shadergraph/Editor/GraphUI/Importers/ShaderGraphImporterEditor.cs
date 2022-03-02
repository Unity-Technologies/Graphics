using System;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Generation;
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
                //var graph = GraphUtil.OpenGraph((target as NewShaderGraphImporter).assetPath);
                var graph = GraphDelta.GraphUtil.OpenGraph(importer.assetPath);
                var reg = Registry.Default.DefaultRegistry.CreateDefaultRegistry();
                var key = Registry.Registry.ResolveKey<Registry.Default.DefaultContext>();
                var node = graph.GetNodeReader(key.Name);
                var shaderCode = Interpreter.GetShaderForNode(node, graph, reg);

                // check for string

                string assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                string path = String.Format("Temp/GeneratedFromGraph-{0}.shader", assetName.Replace(" ", ""));

                if (Utils.GraphUtil.WriteToFile(path, shaderCode))
                    Utils.GraphUtil.OpenFile(path);

                Debug.Log(shaderCode);
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
            var assetModel = AssetDatabase.LoadAssetAtPath(path, typeof(ShaderGraphAssetModel)) as ShaderGraphAssetModel;
            if(assetModel == null)
            {
                return false;
            }
            return ShowWindow(path, assetModel);
        }

        private static bool ShowWindow(string path, ShaderGraphAssetModel model)
        {
            var shaderGraphEditorWindow = GraphViewEditorWindow.FindOrCreateGraphWindow<ShaderGraphEditorWindow>();
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
