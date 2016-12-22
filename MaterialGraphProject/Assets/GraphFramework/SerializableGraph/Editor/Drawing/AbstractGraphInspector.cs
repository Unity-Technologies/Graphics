using System.Linq;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public abstract class AbstractGraphInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = target as IGraphAsset;
            if (asset == null)
                return;

            var selectedNodes = asset.drawingData.selection.Select(asset.graph.GetNodeFromGuid);
            EditorGUILayout.LabelField("Selected nodes", string.Join(", ", selectedNodes.Select(x => x.name).ToArray()));
        }
    }
}
