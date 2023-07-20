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
    /// <summary>
    /// Class <c>Renderer2DData</c> contains resources for a <c>Renderer2D</c>.
    /// </summary>
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.Universal", "Unity.RenderPipelines.Universal.Runtime")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DRendererData-overview.html")]
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

        [SerializeField, Reload("Shaders/2D/Light2D.shader")]
        Shader m_LightShader = null;

        [SerializeField, Reload("Shaders/Utils/CoreBlit.shader")]
        Shader m_CoreBlitShader = null;

        [SerializeField, Reload("Shaders/Utils/CoreBlitColorAndDepth.shader")]
        Shader m_CoreBlitColorAndDepthPS = null;

        [SerializeField, Reload("Shaders/Utils/BlitHDROverlay.shader")]
        Shader m_BlitHDROverlay;

        [SerializeField, Reload("Shaders/Utils/Sampling.shader")]
        Shader m_SamplingShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Projected.shader")]
        Shader m_ProjectedShadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Shadow-Sprite.shader")]
        Shader m_SpriteShadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Unshadow-Sprite.shader")]
        Shader m_SpriteUnshadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Shadow-Geometry.shader")]
        Shader m_GeometryShadowShader = null;

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
        internal Shader lightShader => m_LightShader;
        internal Shader coreBlitPS => m_CoreBlitShader;
        internal Shader coreBlitColorAndDepthPS => m_CoreBlitColorAndDepthPS;
        internal Shader blitHDROverlay => m_BlitHDROverlay;
        internal Shader samplingShader => m_SamplingShader;
        internal PostProcessData postProcessData { get => m_PostProcessData; set { m_PostProcessData = value; } }
        internal Shader spriteShadowShader => m_SpriteShadowShader;
        internal Shader spriteUnshadowShader => m_SpriteUnshadowShader;
        internal Shader geometryShadowShader => m_GeometryShadowShader;
        internal Shader geometryUnshadowShader => m_GeometryUnshadowShader;
        internal Shader projectedShadowShader => m_ProjectedShadowShader;
        internal TransparencySortMode transparencySortMode => m_TransparencySortMode;
        internal Vector3 transparencySortAxis => m_TransparencySortAxis;
        internal uint lightRenderTextureMemoryBudget => m_MaxLightRenderTextureCount;
        internal uint shadowRenderTextureMemoryBudget => m_MaxShadowRenderTextureCount;
        internal bool useCameraSortingLayerTexture => m_UseCameraSortingLayersTexture;
        internal int cameraSortingLayerTextureBound => m_CameraSortingLayersTextureBound;
        internal Downsampling cameraSortingLayerDownsamplingMethod => m_CameraSortingLayerDownsamplingMethod;

        /// <summary>
        /// Creates the instance of the Renderer2D.
        /// </summary>
        /// <returns>The instance of Renderer2D</returns>
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

        internal void Dispose()
        {
            for (var i = 0; i < m_LightBlendStyles.Length; ++i)
                m_LightBlendStyles[i].renderTargetHandle?.Release();

            foreach(var mat in lightMaterials)
                CoreUtils.Destroy(mat.Value);

            lightMaterials.Clear();

            CoreUtils.Destroy(spriteSelfShadowMaterial);
            CoreUtils.Destroy(spriteUnshadowMaterial);
            CoreUtils.Destroy(geometrySelfShadowMaterial);
            CoreUtils.Destroy(geometryUnshadowMaterial);
            CoreUtils.Destroy(projectedShadowMaterial);
            CoreUtils.Destroy(projectedUnshadowMaterial);
        }

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            for (var i = 0; i < m_LightBlendStyles.Length; ++i)
            {
                m_LightBlendStyles[i].renderTargetHandleId = Shader.PropertyToID($"_ShapeLightTexture{i}");
                m_LightBlendStyles[i].renderTargetHandle = RTHandles.Alloc(m_LightBlendStyles[i].renderTargetHandleId, $"_ShapeLightTexture{i}");
            }

            geometrySelfShadowMaterial = null;
            geometryUnshadowMaterial = null;

            spriteSelfShadowMaterial = null;
            spriteUnshadowMaterial = null;
            projectedShadowMaterial = null;
            projectedUnshadowMaterial = null;
        }

        // transient data
        internal Dictionary<uint, Material> lightMaterials { get; } = new Dictionary<uint, Material>();
        internal Material spriteSelfShadowMaterial { get; set; }
        internal Material spriteUnshadowMaterial { get; set; }
        internal Material geometrySelfShadowMaterial { get; set; }
        internal Material geometryUnshadowMaterial { get; set; }
        internal Material projectedShadowMaterial { get; set; }
        internal Material projectedUnshadowMaterial { get; set; }

        internal RTHandle normalsRenderTarget;
        internal RTHandle cameraSortingLayerRenderTarget;



        // this shouldn've been in RenderingData along with other cull results
        internal ILight2DCullResult lightCullResult { get; set; }
    }
}
