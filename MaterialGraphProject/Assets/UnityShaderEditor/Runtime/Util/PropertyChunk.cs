namespace UnityEngine.MaterialGraph
{
    public abstract class PropertyChunk
    {
        public enum HideState
        {
            Hidden,
            Visible
        }

        private readonly string m_PropertyName;
        private readonly string m_PropertyDescription;
        private readonly HideState m_HideState;

        protected PropertyChunk(string propertyName, string propertyDescription, HideState hideState)
        {
            m_PropertyName = propertyName;
            m_PropertyDescription = propertyDescription;
            m_HideState = hideState;
        }

        public abstract string GetPropertyString();
        public string propertyName { get { return m_PropertyName; } }
        public string propertyDescription { get { return m_PropertyDescription; } }
        public HideState hideState { get { return m_HideState; } }
    }
}
