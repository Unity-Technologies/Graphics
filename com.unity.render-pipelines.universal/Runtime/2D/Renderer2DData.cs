using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Rendering.Universal
{
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [MovedFrom("UnityEngine.Experimental.Rendering.Universal")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DRendererData_overview.html")]
    public partial class Renderer2DData : ScriptableRendererData
    {
        internal enum Renderer2DDefaultMaterialType
        {
            Lit,
            Unlit,
            Custom
        }

        [SerializeField]
        TransparencySortMode m_TransparencySortMode = TransparencySortMode.Default;

        [SerializeField]
        Vector3 m_TransparencySortAxis = Vector3.up;

        [SerializeField]
        float m_HDREmulationScale = 1;

        [SerializeField, Range(0.01f, 1.0f)]
        float m_LightRenderTextureScale = 0.5f;

        [SerializeField, FormerlySerializedAs("m_LightOperations")]
        Light2DBlendStyle[] m_LightBlendStyles = null;

        [SerializeField]
        bool m_UseDepthStencilBuffer = true;

        [SerializeField]
        bool m_UseCameraSortingLayersTexture = false;

        [SerializeField]
        int m_CameraSortingLayersTextureBound = 0;

        [SerializeField]
        Downsampling m_CameraSortingLayerDownsamplingMethod = Downsampling.None;

        [SerializeField]
        uint m_MaxLightRenderTextureCount = 16;

        [SerializeField]
        uint m_MaxShadowRenderTextureCount = 1;

        [SerializeField, Reload("Shaders/2D/Light2D-Shape.shader")]
        Shader m_ShapeLightShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Shape-Volumetric.shader")]
        Shader m_ShapeLightVolumeShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Point.shader")]
        Shader m_PointLightShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Point-Volumetric.shader")]
        Shader m_PointLightVolumeShader = null;

        [SerializeField, Reload("Shaders/Utils/Blit.shader")]
        Shader m_BlitShader = null;

        [SerializeField, Reload("Shaders/Utils/Sampling.shader")]
        Shader m_SamplingShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Projected.shader")]
        Shader m_ProjectedShadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Shadow-Sprite.shader")]
        Shader m_SpriteShadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Unshadow-Sprite.shader")]
        Shader m_SpriteUnshadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Unshadow-Geometry.shader")]
        Shader m_GeometryUnshadowShader = null;

        [SerializeField, Reload("Shaders/Utils/FallbackError.shader")]
        Shader m_FallbackErrorShader;

        [SerializeField]
        PostProcessData m_PostProcessData = null;

        [SerializeField, Reload("Runtime/2D/Data/Textures/FalloffLookupTexture.png")]
        [HideInInspector]
        private Texture2D m_FallOffLookup = null;

        /// <summary>
        /// HDR Emulation Scale allows platforms to use HDR lighting by compressing the number of expressible colors in exchange for extra intensity range.
        /// Scale describes this extra intensity range. Increasing this value too high may cause undesirable banding to occur.
        /// </summary>
        public float hdrEmulationScale => m_HDREmulationScale;
        internal float lightRenderTextureScale => m_LightRenderTextureScale;
        /// <summary>
        /// Returns a list Light2DBlendStyle
        /// </summary>
        public Light2DBlendStyle[] lightBlendStyles => m_LightBlendStyles;
        internal bool useDepthStencilBuffer => m_UseDepthStencilBuffer;
        internal Texture2D fallOffLookup => m_FallOffLookup;
        internal Shader shapeLightShader => m_ShapeLightShader;
        internal Shader shapeLightVolumeShader => m_ShapeLightVolumeShader;
        internal Shader pointLightShader => m_PointLightShader;
        internal Shader pointLightVolumeShader => m_PointLightVolumeShader;
        internal Shader blitShader => m_BlitShader;
        internal Shader samplingShader => m_SamplingShader;
        internal PostProcessData postProcessData { get => m_PostProcessData; set { m_PostProcessData = value; } }
        internal Shader spriteShadowShader => m_SpriteShadowShader;
        internal Shader spriteUnshadowShader => m_SpriteUnshadowShader;
        internal Shader geometryUnshadowShader => m_GeometryUnshadowShader;

        internal Shader projectedShadowShader => m_ProjectedShadowShader;
        internal TransparencySortMode transparencySortMode => m_TransparencySortMode;
        internal Vector3 transparencySortAxis => m_TransparencySortAxis;
        internal uint lightRenderTextureMemoryBudget => m_MaxLightRenderTextureCount;
        internal uint shadowRenderTextureMemoryBudget => m_MaxShadowRenderTextureCount;
        internal bool useCameraSortingLayerTexture => m_UseCameraSortingLayersTexture;
        internal int cameraSortingLayerTextureBound => m_CameraSortingLayersTextureBound;
        internal Downsampling cameraSortingLayerDownsamplingMethod => m_CameraSortingLayerDownsamplingMethod;

        protected override ScriptableRenderer Create()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ReloadAllNullProperties();
            }
#endif
            return new Renderer2D(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            for (var i = 0; i < m_LightBlendStyles.Length; ++i)
            {
                m_LightBlendStyles[i].renderTargetHandle.Init($"_ShapeLightTexture{i}");
            }

            normalsRenderTarget.Init("_NormalMap");
            shadowsRenderTarget.Init("_ShadowTex");

            spriteSelfShadowMaterial = null;
            spriteUnshadowMaterial = null;
            projectedShadowMaterial = null;
            stencilOnlyShadowMaterial = null;
        }

        // transient data
        internal Dictionary<uint, Material> lightMaterials { get; } = new Dictionary<uint, Material>();
        internal Material[] spriteSelfShadowMaterial { get; set; }
        internal Material[] spriteUnshadowMaterial { get; set; }
        internal Material[] geometryUnshadowMaterial { get; set; }

        internal Material[] projectedShadowMaterial { get; set; }
        internal Material[] stencilOnlyShadowMaterial { get; set; }

        internal bool isNormalsRenderTargetValid { get; set; }
        internal float normalsRenderTargetScale { get; set; }
        internal RenderTargetHandle normalsRenderTarget;
        internal RenderTargetHandle shadowsRenderTarget;
        internal RenderTargetHandle cameraSortingLayerRenderTarget;

        // this shouldn've been in RenderingData along with other cull results
        internal ILight2DCullResult lightCullResult { get; set; }
    }
}
