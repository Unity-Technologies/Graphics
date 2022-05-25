using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [CustomEditor(typeof(ShaderSubGraphAssetImporter))]
    public class ShaderSubGraphAssetImporterEditor : AssetImporterEditor
    {
        [OnOpenAsset(0)]
        public static bool OnOpenShaderSubGraph(int instanceID, int line)
        {
            string path = AssetDatabase.GetAssetPath(instanceID);
            if (!AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(path))
            {
                return false;
            }

            var assetModel = ShaderGraphAssetUtils.HandleLoad(path);
            return ShowWindow(path, assetModel);
        }

        private static bool ShowWindow(string path, ShaderGraphAsset model)
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
