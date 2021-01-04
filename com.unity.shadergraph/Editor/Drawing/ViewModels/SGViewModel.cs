namespace UnityEditor.ShaderGraph
{
    abstract class SGViewModel
    {
        GraphData m_Model;
        public GraphData model
        {
            get => m_Model;
            private set
            {
                if (model != value)
                {
                    m_Model = value;
                    ConstructFromModel(model);
                }
            }
        }

        public abstract void ConstructFromModel(GraphData graphData);
        public abstract void WriteToModel();
    }
}
