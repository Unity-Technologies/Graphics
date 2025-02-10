#if UNITY_EDITOR
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Editor Shaders", Order = 1000), HideInInspector]
    class UniversalRenderPipelineEditorShaders : IRenderPipelineResources
    {
        public int version => 0;

        #region Autodesk
        [Header("Autodesk")]
        [SerializeField]
        [ResourcePath("Shaders/AutodeskInteractive/AutodeskInteractive.shadergraph")]
        private Shader m_AutodeskInteractive;

        public Shader autodeskInteractiveShader
        {
            get => m_AutodeskInteractive;
            set => this.SetValueAndNotify(ref m_AutodeskInteractive, value);
        }

        [SerializeField]
        [ResourcePath("Shaders/AutodeskInteractive/AutodeskInteractiveTransparent.shadergraph")]
        private Shader m_AutodeskInteractiveTransparent;

        public Shader autodeskInteractiveTransparentShader
        {
            get => m_AutodeskInteractiveTransparent;
            set => this.SetValueAndNotify(ref m_AutodeskInteractiveTransparent, value);
        }

        [SerializeField]
        [ResourcePath("Shaders/AutodeskInteractive/AutodeskInteractiveMasked.shadergraph")]
        private Shader m_AutodeskInteractiveMasked;

        public Shader autodeskInteractiveMaskedShader
        {
            get => m_AutodeskInteractiveMasked;
            set => this.SetValueAndNotify(ref m_AutodeskInteractiveMasked, value);
        }
        #endregion

        #region SpeedTree
        [Header("SpeedTree")]
        [SerializeField]
        [ResourcePath("Shaders/Nature/SpeedTree7.shader")]
        private Shader m_DefaultSpeedTree7Shader;

        public Shader defaultSpeedTree7Shader
        {
            get => m_DefaultSpeedTree7Shader;
            set => this.SetValueAndNotify(ref m_DefaultSpeedTree7Shader, value);
        }

        [SerializeField]
        [ResourcePath("Shaders/Nature/SpeedTree8_PBRLit.shadergraph")]
        private Shader m_DefaultSpeedTree8Shader;

        public Shader defaultSpeedTree8Shader
        {
            get => m_DefaultSpeedTree8Shader;
            set => this.SetValueAndNotify(ref m_DefaultSpeedTree8Shader, value);
        }

        [SerializeField]
        [ResourcePath("Shaders/Nature/SpeedTree9_URP.shadergraph")]
        private Shader m_DefaultSpeedTree9Shader;

        public Shader defaultSpeedTree9Shader
        {
            get => m_DefaultSpeedTree9Shader;
            set => this.SetValueAndNotify(ref m_DefaultSpeedTree9Shader, value);
        }
        #endregion
    }
}
#endif
