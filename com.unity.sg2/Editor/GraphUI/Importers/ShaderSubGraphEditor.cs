using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(ShaderSubGraphImporter))]
    public class ShaderSubGraphEditor : AssetImporterEditor
    {
        [OnOpenAsset(0)]
        public static bool OnOpenShaderSubGraph(int instanceID, int line)
        {
            string path = AssetDatabase.GetAssetPath(instanceID);
            if (!AssetDatabase.LoadAssetAtPath<ShaderSubGraphAsset>(path))
            {
                return false;
            }

            var assetModel = ShaderSubGraphAsset.HandleLoad(path);
            return ShowWindow(path, assetModel);
        }

        private static bool ShowWindow(string path, ShaderGraphAssetModel model)
        {
            // Prevents the same graph asset from being opened in two separate editor windows
            var existingEditorWindows = (ShaderGraphEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(ShaderGraphEditorWindow));
            foreach (var existingEditorWindow in existingEditorWindows)
            {
                if (UnityEngine.Object.ReferenceEquals(existingEditorWindow.GraphTool.ToolState.CurrentGraph.GetGraphAsset(), model))
                    return true;
            }

            var shaderGraphEditorWindow = EditorWindow.CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
            if (shaderGraphEditorWindow == null)
            {
                return false;
            }
            shaderGraphEditorWindow.Show();
            shaderGraphEditorWindow.Focus();
            shaderGraphEditorWindow.SetCurrentSelection(model, GraphViewEditorWindow.OpenMode.OpenAndFocus);
            return true;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("WIP", MessageType.Info);
            ApplyRevertGUI();
        }

    }
}
