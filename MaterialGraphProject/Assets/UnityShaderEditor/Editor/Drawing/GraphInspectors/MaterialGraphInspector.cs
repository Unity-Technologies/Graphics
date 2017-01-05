using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    [CustomEditor(typeof(MaterialGraphAsset))]
    public class MaterialGraphInspector : AbstractMaterialGraphInspector
    {
        private UnityEngine.MaterialGraph.MaterialGraph materialGraph
        {
            get { return m_GraphAsset.graph as UnityEngine.MaterialGraph.MaterialGraph; }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            previewNode = materialGraph.masterNode as AbstractMaterialNode;
        }
    }
}
