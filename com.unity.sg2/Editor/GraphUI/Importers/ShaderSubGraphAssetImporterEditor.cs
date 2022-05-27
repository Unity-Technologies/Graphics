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
            var graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(path);
            if (!graphAsset)
            {
                return false;
            }
            return ShowWindow(path, graphAsset);
        }

        private static bool ShowWindow(string path, ShaderGraphAsset model)
        {
            ShaderGraphEditorWindow shaderGraphEditorWindow = null;

            // Prevents the same graph asset from being opened in two separate editor windows
            var existingEditorWindows = (ShaderGraphEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(ShaderGraphEditorWindow));
            foreach (var existingEditorWindow in existingEditorWindows)
            {
                if (UnityEngine.Object.ReferenceEquals(existingEditorWindow.GraphTool.ToolState.CurrentGraph.GetGraphAsset(), model))
                {
                    shaderGraphEditorWindow = existingEditorWindow;
                    break;
                }
            }

            if (shaderGraphEditorWindow == null)
            {
                shaderGraphEditorWindow = EditorWindow.CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
                if (shaderGraphEditorWindow == null)
                {
                    return false;
                }
                AssetDatabase.ImportAsset(path);
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
