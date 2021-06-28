using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace GtfPlayground
{
    public class PlaygroundGraphAssetModel : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(PlaygroundGraphModel);

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is not PlaygroundGraphAssetModel) return false;

            var path = AssetDatabase.GetAssetPath(instanceId);
            var asset = AssetDatabase.LoadAssetAtPath<PlaygroundGraphAssetModel>(path);
            if (asset == null) return false;

            var window = GraphViewEditorWindow.FindOrCreateGraphWindow<PlaygroundGraphWindow>();
            return window != null;
        }
    }
}