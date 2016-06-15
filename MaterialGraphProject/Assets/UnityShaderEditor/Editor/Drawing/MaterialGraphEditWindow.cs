using UnityEditor.Graphing.Drawing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    public class MaterialGraphEditWindow : AbstractGraphEditWindow<IMaterialGraphAsset>
    {
        [MenuItem("Window/Material Editor")]
        public static void OpenMenu()
        {
            GetWindow<MaterialGraphEditWindow>();
        }
    }
}
