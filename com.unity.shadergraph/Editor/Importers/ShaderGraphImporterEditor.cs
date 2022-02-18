using System;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(ShaderGraphImporter))]
    class ShaderGraphImporterEditor : ScriptedImporterEditor
    {
        MaterialEditor materialEditor = null;
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("WIP", MessageType.Info);

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
