using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: 2D Renderer", Order = 1000), HideInInspector]
    class Renderer2DResources : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 0;

        /// <summary>Version of the resource. </summary>
        public int version => m_Version;
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, ResourcePath("Shaders/2D/Light2D.shader")]
        Shader m_LightShader;

        internal Shader lightShader
        {
            get => m_LightShader;
            set => this.SetValueAndNotify(ref m_LightShader, value, nameof(m_LightShader));
        }

        [SerializeField, ResourcePath("Shaders/2D/Shadow2D-Projected.shader")]
        Shader m_ProjectedShadowShader;
        internal Shader projectedShadowShader
        {
            get => m_ProjectedShadowShader;
            set => this.SetValueAndNotify(ref m_ProjectedShadowShader, value, nameof(m_ProjectedShadowShader));
        }

        [SerializeField, ResourcePath("Shaders/2D/Shadow2D-Shadow-Sprite.shader")]
        Shader m_SpriteShadowShader;

        internal Shader spriteShadowShader
        {
            get => m_SpriteShadowShader;
            set => this.SetValueAndNotify(ref m_SpriteShadowShader, value, nameof(m_SpriteShadowShader));
        }

        [SerializeField, ResourcePath("Shaders/2D/Shadow2D-Unshadow-Sprite.shader")]
        Shader m_SpriteUnshadowShader;

        internal Shader spriteUnshadowShader
        {
            get => m_SpriteUnshadowShader;
            set => this.SetValueAndNotify(ref m_SpriteUnshadowShader, value, nameof(m_SpriteUnshadowShader));
        }

        [SerializeField, ResourcePath("Shaders/2D/Shadow2D-Shadow-Geometry.shader")]
        Shader m_GeometryShadowShader;
        internal Shader geometryShadowShader
        {
            get => m_GeometryShadowShader;
            set => this.SetValueAndNotify(ref m_GeometryShadowShader, value, nameof(m_GeometryShadowShader));
        }

        [SerializeField, ResourcePath("Shaders/2D/Shadow2D-Unshadow-Geometry.shader")]
        Shader m_GeometryUnshadowShader;

        internal Shader geometryUnshadowShader
        {
            get => m_GeometryUnshadowShader;
            set => this.SetValueAndNotify(ref m_GeometryUnshadowShader, value, nameof(m_GeometryUnshadowShader));
        }

        [SerializeField, ResourcePath("Runtime/2D/Data/Textures/FalloffLookupTexture.png")]
        [HideInInspector]
        private Texture2D m_FallOffLookup;

        internal Texture2D fallOffLookup
        {
            get => m_FallOffLookup;
            set => this.SetValueAndNotify(ref m_FallOffLookup, value, nameof(m_FallOffLookup));
        }

#if UNITY_EDITOR
        [SerializeField, ResourcePath("Runtime/Materials/Sprite-Lit-Default.mat")]
        Material m_DefaultCustomMaterial = null;
        internal Material defaultCustomMaterial
        {
            get => m_DefaultCustomMaterial;
            set => this.SetValueAndNotify(ref m_DefaultCustomMaterial, value, nameof(m_DefaultCustomMaterial));
        }

        [SerializeField, ResourcePath("Runtime/Materials/Sprite-Lit-Default.mat")]
        Material m_DefaultLitMaterial = null;
        internal Material defaultLitMaterial
        {
            get => m_DefaultLitMaterial;
            set => this.SetValueAndNotify(ref m_DefaultLitMaterial, value, nameof(m_DefaultLitMaterial));
        }

        [SerializeField, ResourcePath("Runtime/Materials/Sprite-Unlit-Default.mat")]
        Material m_DefaultUnlitMaterial = null;
        internal Material defaultUnlitMaterial
        {
            get => m_DefaultUnlitMaterial;
            set => this.SetValueAndNotify(ref m_DefaultUnlitMaterial, value, nameof(m_DefaultUnlitMaterial));
        }

        [SerializeField, ResourcePath("Runtime/Materials/SpriteMask-Default.mat")]
        Material m_DefaultMaskMaterial = null;
        internal Material defaultMaskMaterial
        {
            get => m_DefaultMaskMaterial;
            set => this.SetValueAndNotify(ref m_DefaultMaskMaterial, value, nameof(m_DefaultMaskMaterial));
        }
#endif

    }
}
