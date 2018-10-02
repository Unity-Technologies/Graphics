using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LightweightRenderPipelineEditorResources : ScriptableObject
    {
        [FormerlySerializedAs("DefaultMaterial"),SerializeField]
        Material m_LitMaterial = null;

        [FormerlySerializedAs("DefaultParticleMaterial"),SerializeField]
        Material m_ParticleLitMaterial = null;

        [FormerlySerializedAs("DefaultTerrainMaterial"),SerializeField]
        Material m_TerrainLitMaterial = null;

        [FormerlySerializedAs("AutodeskInteractiveShader"), SerializeField]
        private Shader m_AutodeskInteractiveShader = null;

        [FormerlySerializedAs("AutodeskInteractiveTransparentShader"), SerializeField]
        private Shader m_AutodeskInteractiveTransparentShader = null;

        [FormerlySerializedAs("AutodeskInteractiveMaskedShader"), SerializeField]
        private Shader m_AutodeskInteractiveMaskedShader = null;

        public Material litMaterial
        {
            get { return m_LitMaterial; }
        }

        public Material particleLitMaterial
        {
            get { return m_ParticleLitMaterial; }
        }

        public Material terrainLitMaterial
        {
            get { return m_TerrainLitMaterial; }
        }

        public Shader autodeskInteractiveShader
        {
            get { return m_AutodeskInteractiveShader; }
        }

        public Shader autodeskInteractiveTransparentShader
        {
            get { return m_AutodeskInteractiveTransparentShader; }
        }

        public Shader autodeskInteractiveMaskedShader
        {
            get { return m_AutodeskInteractiveMaskedShader; }
        }

    }
}
