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
    //TODO: Check if this is okay.
    //[MovedFrom("UnityEngine.Experimental.Rendering.Universal")]
    //[MovedFrom(false, sourceClassName: "Renderer2DData")]
    [Obsolete("Renderer2DData no longer uses scriptable object.")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DRendererData_overview.html")]
    public class Renderer2DDataAssetLegacy : ScriptableRendererDataAssetLegacy
    {
        internal enum Renderer2DDefaultMaterialType
        {
            Lit,
            Unlit,
            Custom
        }
        [SerializeField]
        internal TransparencySortMode m_TransparencySortMode = TransparencySortMode.Default;

        [SerializeField]
        internal Vector3 m_TransparencySortAxis = Vector3.up;

        [SerializeField]
        internal float m_HDREmulationScale = 1;

        [SerializeField, Range(0.01f, 1.0f)]
        internal float m_LightRenderTextureScale = 0.5f;

        [SerializeField, FormerlySerializedAs("m_LightOperations")]
        internal Light2DBlendStyle[] m_LightBlendStyles = null;

        [SerializeField]
        internal bool m_UseDepthStencilBuffer = true;

        [SerializeField]
        internal bool m_UseCameraSortingLayersTexture = false;

        [SerializeField]
        internal int m_CameraSortingLayersTextureBound = 0;

        [SerializeField]
        internal Downsampling m_CameraSortingLayerDownsamplingMethod = Downsampling.None;

        [SerializeField]
        internal uint m_MaxLightRenderTextureCount = 16;

        [SerializeField]
        internal uint m_MaxShadowRenderTextureCount = 1;

        [SerializeField, Reload("Shaders/2D/Light2D-Shape.shader")]
        internal Shader m_ShapeLightShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Shape-Volumetric.shader")]
        internal Shader m_ShapeLightVolumeShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Point.shader")]
        internal Shader m_PointLightShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Point-Volumetric.shader")]
        internal Shader m_PointLightVolumeShader = null;

        [SerializeField, Reload("Shaders/Utils/Blit.shader")]
        internal Shader m_BlitShader = null;

        [SerializeField, Reload("Shaders/Utils/Sampling.shader")]
        internal Shader m_SamplingShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Projected.shader")]
        internal Shader m_ProjectedShadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Shadow-Sprite.shader")]
        internal Shader m_SpriteShadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Unshadow-Sprite.shader")]
        internal Shader m_SpriteUnshadowShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2D-Unshadow-Geometry.shader")]
        internal Shader m_GeometryUnshadowShader = null;

        [SerializeField, Reload("Shaders/Utils/FallbackError.shader")]
        internal Shader m_FallbackErrorShader;

        [SerializeField]
        internal PostProcessData m_PostProcessData = null;

        [SerializeField, Reload("Runtime/2D/Data/Textures/FalloffLookupTexture.png")]
        [HideInInspector]
        internal Texture2D m_FallOffLookup = null;

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




        // transient data
        internal Dictionary<uint, Material> lightMaterials { get; } = new Dictionary<uint, Material>();
        internal Material[] spriteSelfShadowMaterial { get; set; }
        internal Material[] spriteUnshadowMaterial { get; set; }
        internal Material[] geometryUnshadowMaterial { get; set; }

        internal Material[] projectedShadowMaterial { get; set; }
        internal Material[] stencilOnlyShadowMaterial { get; set; }

        internal bool isNormalsRenderTargetValid { get; set; }
        internal float normalsRenderTargetScale { get; set; }
        internal RTHandle normalsRenderTarget;
        internal int normalsRenderTargetId;
        internal RTHandle shadowsRenderTarget;
        internal int shadowsRenderTargetId;
        internal RTHandle cameraSortingLayerRenderTarget;
        internal int cameraSortingLayerRenderTargetId;

        // this shouldn've been in RenderingData along with other cull results
        internal ILight2DCullResult lightCullResult { get; set; }

#if UNITY_EDITOR
        [SerializeField]
        internal Renderer2DDefaultMaterialType m_DefaultMaterialType = Renderer2DDefaultMaterialType.Lit;

        [SerializeField, Reload("Runtime/Materials/Sprite-Lit-Default.mat")]
        internal Material m_DefaultCustomMaterial = null;

        [SerializeField, Reload("Runtime/Materials/Sprite-Lit-Default.mat")]
        internal Material m_DefaultLitMaterial = null;

        [SerializeField, Reload("Runtime/Materials/Sprite-Unlit-Default.mat")]
        internal Material m_DefaultUnlitMaterial = null;

        [SerializeField, Reload("Runtime/Materials/SpriteMask-Default.mat")]
        internal Material m_DefaultMaskMaterial = null;
#endif

        protected override ScriptableRenderer Create()
        {
            throw new NotImplementedException();
        }

        protected override ScriptableRendererData UpgradeRendererWithoutAsset(UniversalRenderPipelineAsset URPAsset)
        {
            var renderer = new Renderer2DData();
            renderer.UpdateFromAssetLegacy(this);
            renderer.UpdateFromAssetLegacyEditor(this);
            return renderer;
        }
    }
}
