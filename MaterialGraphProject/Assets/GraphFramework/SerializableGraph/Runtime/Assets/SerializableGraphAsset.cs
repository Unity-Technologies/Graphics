namespace UnityEngine.Graphing
{
    public class SerializableGraphAsset : ScriptableObject, IGraphAsset, IOnAssetEnabled
    {
        [SerializeField]
        private SerializableGraph m_Graph = new SerializableGraph();

        public IGraph graph
        {
            get { return m_Graph; }
        }

        public bool shouldRepaint
        {
            get { return false; }
        }

        public ScriptableObject GetScriptableObject()
        {
            return this;
        }

        public void OnEnable()
        {
            graph.OnEnable();
        }

        [SerializeField]
        private GraphDrawingData m_DrawingData = new GraphDrawingData();

        public GraphDrawingData drawingData
        {
            get { return m_DrawingData; }
        }
    }
}
