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

        public FilterType filterType;
        public Color maskColor;
        public float keyThreshold = 0.8f;
        public float keyTolerance = 0.5f;

        [Range(0.0f, 1.0f)]
        public float spillRemoval = 0.0f;
        public Texture alphaMask;

        static public CompositionFilter Create(FilterType type)
        {
            var newFilter = new CompositionFilter();
            newFilter.filterType = type;
            return newFilter;
        }
    }
}
