using System;
using System.Diagnostics;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    [Obsolete("Decal no longer inherits from scriptable object and is not used as an asset.")]
    [DisallowMultipleRendererFeature("Decal (Asset Legacy)")]
    [Tooltip("With this Renderer Feature, Unity can project specific Materials (decals) onto other objects in the Scene.")]
    [URPHelpURL("renderer-feature-decal")]
    internal class DecalRendererFeatureAssetLegacy : ScriptableRendererFeatureAssetLegacy
    {
        [SerializeField]
        internal DecalSettings m_Settings = new DecalSettings();

        [SerializeField]
        [HideInInspector]
        [Reload("Shaders/Utils/CopyDepth.shader")]
        internal Shader m_CopyDepthPS;

        [SerializeField]
        [HideInInspector]
        [Reload("Runtime/Decal/DBuffer/DBufferClear.shader")]
        internal Shader m_DBufferClear;

        internal DecalTechnique m_Technique = DecalTechnique.Invalid;
        internal DBufferSettings m_DBufferSettings;
        internal DecalScreenSpaceSettings m_ScreenSpaceSettings;
        internal bool m_RecreateSystems;

        internal CopyDepthPass m_CopyDepthPass;
        internal DecalPreviewPass m_DecalPreviewPass;
        internal Material m_CopyDepthMaterial;

        // Entities
        internal DecalEntityManager m_DecalEntityManager;
        internal DecalUpdateCachedSystem m_DecalUpdateCachedSystem;
        internal DecalUpdateCullingGroupSystem m_DecalUpdateCullingGroupSystem;
        internal DecalUpdateCulledSystem m_DecalUpdateCulledSystem;
        internal DecalCreateDrawCallSystem m_DecalCreateDrawCallSystem;
        internal DecalDrawErrorSystem m_DrawErrorSystem;

        // DBuffer
        internal DBufferRenderPass m_DBufferRenderPass;
        internal DecalForwardEmissivePass m_ForwardEmissivePass;
        internal DecalDrawDBufferSystem m_DecalDrawDBufferSystem;
        internal DecalDrawFowardEmissiveSystem m_DecalDrawForwardEmissiveSystem;
        internal Material m_DBufferClearMaterial;

        // Screen Space
        internal DecalScreenSpaceRenderPass m_ScreenSpaceDecalRenderPass;
        internal DecalDrawScreenSpaceSystem m_DecalDrawScreenSpaceSystem;
        internal DecalSkipCulledSystem m_DecalSkipCulledSystem;

        // GBuffer
        internal DecalGBufferRenderPass m_GBufferRenderPass;
        internal DecalDrawGBufferSystem m_DrawGBufferSystem;
        internal DeferredLights m_DeferredLights;

        public override ScriptableRendererFeature UpgradeToRendererFeatureWithoutAsset()
        {
            return DecalRendererFeature.UpgradeFrom(this);
        }
    }
}
