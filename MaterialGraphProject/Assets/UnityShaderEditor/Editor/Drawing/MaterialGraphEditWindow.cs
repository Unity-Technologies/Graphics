using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class MaterialGraphEditWindow : AbstractGraphEditWindow<IMaterialGraphAsset>
    {
        [MenuItem("Window/Material Editor")]
        public static void OpenMenu()
        {
            GetWindow<MaterialGraphEditWindow>();
        }

        public override AbstractGraphPresenter CreateDataSource()
        {
            return CreateInstance<MaterialGraphPresenter>();
        }

        public override GraphView CreateGraphView()
        {
            return new MaterialGraphView();
        }
    }
}
