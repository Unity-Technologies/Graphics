using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class GraphEditWindow : AbstractGraphEditWindow
    {
        public override IGraphAsset inMemoryAsset { get; set; }

        public override Object selected { get; set; }

        public GraphEditWindow(string path)
        {

        }
    }
}
