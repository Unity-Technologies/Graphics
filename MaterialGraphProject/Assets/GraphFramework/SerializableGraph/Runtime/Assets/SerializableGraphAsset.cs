namespace UnityEngine.Graphing
{
    public class SerializableGraphAsset : ScriptableObject, IGraphAsset, IOnAssetEnabled
    {
        [SerializeField]
        protected SerializableGraph m_Graph = new SerializableGraph();
        
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
    }
}
