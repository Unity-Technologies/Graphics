namespace UnityEngine.Graphing
{
    public interface IGraphAsset
    {
        IGraph graph { get; }
        bool shouldRepaint { get; }
    }

    public class SerializableGraphAsset : ScriptableObject, IGraphAsset
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
    }
}
