using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    [System.Serializable]
    internal class CompositionFilter
    {
        public enum FilterType
        {
            CHROMA_KEYING = 0,
            ALPHA_MASK
        }

        //TODO: inheritance?
        public int m_Type;
        public Color m_MaskColor;
        public float m_KeyThreshold = 0.8f;
        public float m_KeyTolerance = 0.5f;
        public float m_SpillRemoval = 0.0f;
        public Texture m_AlphaMask;

        static public CompositionFilter Create(FilterType type)
        {
            var newFilter = new CompositionFilter();
            newFilter.m_Type = (int)type;
            return newFilter;
        }
    }
}
