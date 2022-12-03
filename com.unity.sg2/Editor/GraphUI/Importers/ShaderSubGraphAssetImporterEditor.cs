using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using Unity.GraphToolsFoundation.Editor;
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
            //string path = AssetDatabase.GetAssetPath(instanceID);
            //var graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(path);
            //if (!graphAsset)
            //{
            //    return false;
            //}
            //return ShowWindow(path, graphAsset);

            return false;
        }


        // TODO (Brett) Look at whether this needs to return anything.
        private static bool ShowWindow(string path, ShaderGraphAsset model)
        {
            GraphViewEditorWindow.ShowGraphInExistingOrNewWindow<ShaderGraphEditorWindow>(model);
            return true;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("WIP", MessageType.Info);
            ApplyRevertGUI();
        }

    }
}
