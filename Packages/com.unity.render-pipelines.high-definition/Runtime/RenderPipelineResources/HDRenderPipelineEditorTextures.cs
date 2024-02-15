#if UNITY_EDITOR
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Editor Textures", Order = 1000), HideInInspector]
    class HDRenderPipelineEditorTextures : IRenderPipelineResources
    {
        public int version => 0;

        [SerializeField]
        [ResourcePath("Runtime/RenderPipelineResources/Texture/MoonAlbedo.png")]
        private Texture m_MoonAlbedo = null;

        public virtual Texture moonAlbedo
        {
            get => m_MoonAlbedo;
            set => this.SetValueAndNotify(ref m_MoonAlbedo, value, nameof(m_MoonAlbedo));
        }
    }
}
#endif
