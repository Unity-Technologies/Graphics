#if UNITY_EDITOR
using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [HideInInspector]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Category("Resources/Editor Textures")]
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
