using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Experimental.Rendering.Universal
{
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [MovedFrom("UnityEngine.Experimental.Rendering.LWRP")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DRendererData_overview.html")]
    public partial class Renderer2DData : ScriptableRendererData
    {
        public enum Renderer2DDefaultMaterialType
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

        [SerializeField, Reload("Shaders/2D/ShadowGroup2D.shader")]
        Shader m_ShadowGroupShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2DRemoveSelf.shader")]
        Shader m_RemoveSelfShadowShader = null;

        [SerializeField, Reload("Shaders/Utils/FallbackError.shader")]
        Shader m_FallbackErrorShader;

        [SerializeField]
        PostProcessData m_PostProcessData = null;

        [SerializeField, Reload("Runtime/2D/Data/Textures/FalloffLookupTexture.png")]
        [HideInInspector]
        private Texture2D m_FallOffLookup = null;

        public float hdrEmulationScale => m_HDREmulationScale;
        internal float lightRenderTextureScale => m_LightRenderTextureScale;
        public Light2DBlendStyle[] lightBlendStyles => m_LightBlendStyles;
        internal bool useDepthStencilBuffer => m_UseDepthStencilBuffer;
        internal Texture2D fallOffLookup => m_FallOffLookup;
        internal Shader shapeLightShader => m_ShapeLightShader;
        internal Shader shapeLightVolumeShader => m_ShapeLightVolumeShader;
        internal Shader pointLightShader => m_PointLightShader;
        internal Shader pointLightVolumeShader => m_PointLightVolumeShader;
        internal Shader blitShader => m_BlitShader;
        internal Shader samplingShader => m_SamplingShader;
        internal Shader shadowGroupShader => m_ShadowGroupShader;
        internal Shader removeSelfShadowShader => m_RemoveSelfShadowShader;
        internal PostProcessData postProcessData { get => m_PostProcessData; set { m_PostProcessData = value; } }
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

            const int totalMaterials = 256;
            if (shadowMaterials == null || shadowMaterials.Length == 0)
                shadowMaterials = new Material[totalMaterials];
            if (removeSelfShadowMaterials == null || removeSelfShadowMaterials.Length == 0)
                removeSelfShadowMaterials = new Material[totalMaterials];
        }

        // transient data
        internal Dictionary<uint, Material> lightMaterials { get; } = new Dictionary<uint, Material>();
        internal Material[] shadowMaterials { get; private set; }
        internal Material[] removeSelfShadowMaterials { get; private set; }

        internal bool isNormalsRenderTargetValid { get; set; }
        internal float normalsRenderTargetScale { get; set; }
        internal RenderTargetHandle normalsRenderTarget;
        internal RenderTargetHandle shadowsRenderTarget;
        internal RenderTargetHandle cameraSortingLayerRenderTarget;

        // this shouldn've been in RenderingData along with other cull results
        internal ILight2DCullResult lightCullResult { get; set; }
    }
}
