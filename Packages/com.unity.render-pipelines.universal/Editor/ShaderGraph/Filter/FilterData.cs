using UnityEngine;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // Per-graph filter settings: read/write margins.
    internal class FilterData : JsonObject
    {
        public enum Version
        {
            Initial,
        }

        [SerializeField] Version m_Version = Version.Initial;
        public Version version
        {
            get => m_Version;
            set => m_Version = value;
        }

        // Uniform margin on all four sides (v1). Parameter-dependent margins would need a
        // generated delegate, not just data -- stage-2 item, not attempted here.
        [SerializeField] float m_ReadMargin;
        public float readMargin
        {
            get => m_ReadMargin;
            set => m_ReadMargin = value;
        }

        [SerializeField] float m_WriteMargin;
        public float writeMargin
        {
            get => m_WriteMargin;
            set => m_WriteMargin = value;
        }
    }
}
