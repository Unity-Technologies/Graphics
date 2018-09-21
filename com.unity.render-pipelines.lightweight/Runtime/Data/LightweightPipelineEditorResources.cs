using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LightweightPipelineEditorResources : ScriptableObject
    {
        [FormerlySerializedAs("DefaultMaterial"),SerializeField] Material m_DefaultMaterial = null;
        [FormerlySerializedAs("DefaultParticleMaterial"),SerializeField] Material m_DefaultParticleMaterial = null;
        [FormerlySerializedAs("DefaultTerrainMaterial"),SerializeField] Material m_DefaultTerrainMaterial = null;
        public Shader AutodeskInteractiveShader;
        public Shader AutodeskInteractiveTransparentShader;
        public Shader AutodeskInteractiveMaskedShader;

        public Material defaultMaterial
        {
            get { return m_DefaultMaterial; }
        }

        public Material defaultParticleMaterial
        {
            get { return m_DefaultParticleMaterial; }
        }

        public Material defaultTerrainMaterial
        {
            get { return m_DefaultTerrainMaterial; }
        }
    }
}
