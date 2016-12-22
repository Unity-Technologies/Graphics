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
            EditorGUILayout.LabelField("Selected nodes", string.Join(", ", asset.drawingData.selection.Select(x => asset.graph.GetNodeFromGuid(x).name).ToArray()));
        }
    }
}
