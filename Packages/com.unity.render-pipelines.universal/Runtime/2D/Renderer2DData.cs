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
    [URPHelpURL("urp/2DRendererData-overview")]
    public partial class Renderer2DData : ScriptableRendererData
    {
        internal enum Renderer2DDefaultMaterialType
        {
            Lit,
            Unlit,
            Custom
        }

        [SerializeField]
        LayerMask m_LayerMask = -1;

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

        [SerializeField]
        PostProcessData m_PostProcessData = null;

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
        internal PostProcessData postProcessData { get => m_PostProcessData; set { m_PostProcessData = value; } }
        internal TransparencySortMode transparencySortMode => m_TransparencySortMode;
        internal Vector3 transparencySortAxis => m_TransparencySortAxis;
        internal uint lightRenderTextureMemoryBudget => m_MaxLightRenderTextureCount;
        internal uint shadowRenderTextureMemoryBudget => m_MaxShadowRenderTextureCount;
        internal bool useCameraSortingLayerTexture => m_UseCameraSortingLayersTexture;
        internal int cameraSortingLayerTextureBound => m_CameraSortingLayersTextureBound;
        internal Downsampling cameraSortingLayerDownsamplingMethod => m_CameraSortingLayerDownsamplingMethod;
        internal LayerMask layerMask => m_LayerMask;
        internal bool useRenderingLayers { get { return UniversalRenderPipeline.asset.useRenderingLayers; } }
        internal RenderingLayerUtils.MaskSize renderingLayersMaskSize { get; set; }

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
            UnityEngine.RenderAs2DUtil.InitializeCanRenderAs2D();
            return new Renderer2D(this);
        }

        internal void Dispose()
        {
            UnityEngine.RenderAs2DUtil.DisposeCanRenderAs2D();
            ClearLightMaterialCache();
            ClearShadowMaterialCaches();
        }

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            // Shadow material caches are cleared on enable so editor shader changes
            // force a rebuild on the next render. (Mirrors the previous behavior of
            // null'ing the individual shadow material properties here.)
            ClearShadowMaterialCaches();
        }

        // transient data
        internal Dictionary<uint, Material> lightMaterials { get; } = new Dictionary<uint, Material>();
        internal Dictionary<uint, ShadowRendering.CachedShadowMaterial> spriteShadowMaterials { get; } = new Dictionary<uint, ShadowRendering.CachedShadowMaterial>();
        internal Dictionary<uint, ShadowRendering.CachedShadowMaterial> geometryShadowMaterials { get; } = new Dictionary<uint, ShadowRendering.CachedShadowMaterial>();
        internal Dictionary<uint, ShadowRendering.CachedShadowMaterial> projectedShadowMaterials { get; } = new Dictionary<uint, ShadowRendering.CachedShadowMaterial>();

        /// <summary>
        /// Clears the cached light materials, forcing them to be recreated.
        /// Called when shader variants or settings change.
        /// </summary>
        internal void ClearLightMaterialCache()
        {
            foreach (var mat in lightMaterials.Values)
            {
                if (mat != null)
                    CoreUtils.Destroy(mat);
            }
            lightMaterials.Clear();
        }

        /// <summary>
        /// Clears the cached shadow materials, forcing them to be recreated.
        /// </summary>
        internal void ClearShadowMaterialCaches()
        {
            foreach (var entry in spriteShadowMaterials.Values)
            {
                if (entry.material != null)
                    CoreUtils.Destroy(entry.material);
            }
            spriteShadowMaterials.Clear();

            foreach (var entry in geometryShadowMaterials.Values)
            {
                if (entry.material != null)
                    CoreUtils.Destroy(entry.material);
            }
            geometryShadowMaterials.Clear();

            foreach (var entry in projectedShadowMaterials.Values)
            {
                if (entry.material != null)
                    CoreUtils.Destroy(entry.material);
            }
            projectedShadowMaterials.Clear();
        }

        internal RTHandle normalsRenderTarget;
        internal RTHandle cameraSortingLayerRenderTarget;

        // this shouldn've been in RenderingData along with other cull results
        internal ILight2DCullResult lightCullResult { get; set; }
    }
}
