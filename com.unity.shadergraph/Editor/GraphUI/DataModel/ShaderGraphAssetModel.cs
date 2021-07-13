using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using GraphViewEditorWindow = UnityEditor.GraphToolsFoundation.Overdrive.GraphViewEditorWindow;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class ShaderGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(ShaderGraphModel);

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is not ShaderGraphAssetModel) return false;

            var path = AssetDatabase.GetAssetPath(instanceId);
            var asset = AssetDatabase.LoadAssetAtPath<ShaderGraphAssetModel>(path);
            if (asset == null) return false;

            var window = GraphViewEditorWindow.FindOrCreateGraphWindow<ShaderGraphEditorWindow>();
            return window != null;
        }
    }
}
