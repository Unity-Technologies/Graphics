#if UNITY_EDITOR
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Editor Materials", Order = 1000), HideInInspector]
    class UniversalRenderPipelineEditorMaterials : IRenderPipelineResources
    {
        public int version => 0;

        [SerializeField]
        [ResourcePath("Runtime/Materials/Lit.mat")]
        private Material m_DefaultMaterial;

        public virtual Material defaultMaterial
        {
            get => m_DefaultMaterial;
            set => this.SetValueAndNotify(ref m_DefaultMaterial, value);
        }

        // This is the URP default material for new particle systems, is the closest match to the built-in shader.
        [SerializeField]
        [ResourcePath("Runtime/Materials/ParticlesUnlit.mat")]
        private Material m_DefaultParticleMaterial;

        public virtual Material defaultParticleUnlitMaterial
        {
            get => m_DefaultParticleMaterial;
            set => this.SetValueAndNotify(ref m_DefaultParticleMaterial, value);
        }

        [SerializeField]
        [ResourcePath("Runtime/Materials/ParticlesUnlit.mat")]
        private Material m_DefaultLineMaterial;

        public virtual Material defaultLineMaterial
        {
            get => m_DefaultLineMaterial;
            set => this.SetValueAndNotify(ref m_DefaultLineMaterial, value);
        }

        [SerializeField]
        [ResourcePath("Runtime/Materials/TerrainLit.mat")]
        private Material m_DefaultTerrainMaterial;

        public virtual Material defaultTerrainLitMaterial
        {
            get => m_DefaultTerrainMaterial;
            set => this.SetValueAndNotify(ref m_DefaultTerrainMaterial, value);
        }

        [SerializeField]
        [ResourcePath("Runtime/Materials/Decal.mat")]
        private Material m_DefaultDecalMaterial;

        public virtual Material defaultDecalMaterial
        {
            get => m_DefaultDecalMaterial;
            set => this.SetValueAndNotify(ref m_DefaultDecalMaterial, value);
        }

        [SerializeField]
        [ResourcePath("Runtime/Materials/Sprite-Unlit-Default.mat")]
        private Material m_DefaultSpriteMaterial;

        public virtual Material defaultSpriteMaterial
        {
            get => m_DefaultSpriteMaterial;
            set => this.SetValueAndNotify(ref m_DefaultSpriteMaterial, value);
        }
    }
}
#endif
