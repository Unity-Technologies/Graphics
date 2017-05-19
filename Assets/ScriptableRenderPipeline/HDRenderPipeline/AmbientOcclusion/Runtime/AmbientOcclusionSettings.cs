using UnityEngine;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public sealed class AmbientOcclusionSettings : ScriptableObject
    {
        [Range(0, 2)]
        public float intensity = 1.0f;

        public float radius = 0.5f;

        [Range(1, 32)]
        public int sampleCount = 8;

        public bool downsampling = true;

        // SSAO shader: hidden in inspector.
        [SerializeField]
        Shader m_aoShader;

        public Shader aoShader
        {
            get { return m_aoShader; }
        }
    }
}
