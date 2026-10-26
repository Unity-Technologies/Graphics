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

        [SerializeField, ResourcePath("Shaders/2D/RenderingLayerMask.shader")]
        Shader m_RenderingLayerMaskShader;

        internal Shader renderingLayerMaskShader
        {
            get => m_RenderingLayerMaskShader;
            set => this.SetValueAndNotify(ref m_RenderingLayerMaskShader, value, nameof(m_RenderingLayerMaskShader));
        }

        // The single 2D shadow shader. It declares all five shadow pass roles -- Self,
        // UnshadowMark, UnshadowUnmark, ProjectedSelf and ProjectedUnshadow -- so one shader serves
        // the caster silhouette and the projected body alike, and the SHADOW_SPRITE_CASTER material
        // keyword selects sprite semantics over geometry within the caster passes.
        //
        // The separate m_SpriteShadowShader / m_ProjectedShadowShader (and before them
        // m_GeometryShadowShader) were merged into this one. Declaring every role on a single shader
        // is what lets one custom Material assigned to ShadowCaster2D.material satisfy every phase.
        [SerializeField, ResourcePath("Shaders/2D/Shadow2D.shader")]
        Shader m_ShadowShader;

        internal Shader shadowShader
        {
            get => m_ShadowShader;
            set => this.SetValueAndNotify(ref m_ShadowShader, value, nameof(m_ShadowShader));
        }

        [SerializeField,ResourcePath("Shaders/Utils/CopyDepth.shader")]
        private Shader m_CopyDepthPS;

        /// <summary>
        /// Copy Depth shader.
        /// </summary>
        internal Shader copyDepthPS
        {
            get => m_CopyDepthPS;
            set => this.SetValueAndNotify(ref m_CopyDepthPS, value, nameof(m_CopyDepthPS));
        }

#if UNITY_EDITOR
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



        [SerializeField, ResourcePath("Runtime/Materials/Mesh2D-Lit-Default.mat")]
        Material m_DefaultMesh2DLitMaterial = null;
        internal Material defaultMesh2DLitMaterial
        {
            get => m_DefaultMesh2DLitMaterial;
            set => this.SetValueAndNotify(ref m_DefaultMesh2DLitMaterial, value, nameof(m_DefaultMesh2DLitMaterial));
        }
#endif
    }
}
