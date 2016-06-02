namespace UnityEngine.Graphing
{
    public class SerializableGraphAsset : ScriptableObject
    {
        [SerializeField]
        private SerializableGraph m_Graph = new SerializableGraph();
        
        public SerializableGraph graph
        {
            get { return m_Graph; }
        }
    }
}
