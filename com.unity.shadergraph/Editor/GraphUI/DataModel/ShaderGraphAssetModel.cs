using System;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
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

            var shaderGraphEditorWindow = EditorWindow.CreateWindow<ShaderGraphEditorWindow>(typeof(SceneView), typeof(ShaderGraphEditorWindow));
            shaderGraphEditorWindow.Show();
            shaderGraphEditorWindow.Focus();
            return shaderGraphEditorWindow != null;
        }
    }
}
