using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class GraphEditWindow : AbstractGraphEditWindow<IGraphAsset>
    {
        [MenuItem("Window/Graph Editor")]
        public static void OpenMenu()
        {
            GetWindow<GraphEditWindow>();
        }
    }
}
