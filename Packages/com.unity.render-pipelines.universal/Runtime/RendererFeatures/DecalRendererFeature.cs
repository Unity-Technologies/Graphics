using System.Diagnostics;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal.Internal;
#if UNITY_EDITOR
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif

namespace UnityEngine.Rendering.Universal
{
    internal enum DecalSurfaceData
    {
        [Tooltip("Decals will affect only base color and emission.")]
        Albedo,
        [Tooltip("Decals will affect only base color, normal and emission.")]
        AlbedoNormal,
        [Tooltip("Decals will affect base color, normal, metallic, ambient occlusion, smoothness and emission.")]
        AlbedoNormalMAOS,
    }

    internal enum DecalTechnique
    {
        Invalid,
        DBuffer,
        ScreenSpace,
        GBuffer,
    }

    internal enum DecalTechniqueOption
    {
        [Tooltip("Automatically selects technique based on build platform.")]
        Automatic,
        [Tooltip("Renders decals into DBuffer and then applied during opaque rendering. Requires DepthNormal prepass which makes not viable solution for the tile based renderers common on mobile.")]
        [InspectorName("DBuffer")]
        DBuffer,
        [Tooltip("Renders decals after opaque objects with normal reconstructed from depth. The decals are simply rendered as mesh on top of opaque ones, as result does not support blending per single surface data (etc. normal blending only).")]
        ScreenSpace,
    }

    [System.Serializable]
    internal class DBufferSettings
    {
        public DecalSurfaceData surfaceData = DecalSurfaceData.AlbedoNormalMAOS;
    }

    internal enum DecalNormalBlend
    {
        [Tooltip("Low quality of normal reconstruction (Uses 1 sample).")]
        Low,
        [Tooltip("Medium quality of normal reconstruction (Uses 5 samples).")]
        Medium,
        [Tooltip("High quality of normal reconstruction (Uses 9 samples).")]
        High,
    }

    [System.Serializable]
    internal class DecalScreenSpaceSettings
    {
        public DecalNormalBlend normalBlend = DecalNormalBlend.Low;
        public bool useGBuffer = true;
    }

    [System.Serializable]
    internal class DecalSettings
    {
        public DecalTechniqueOption technique = DecalTechniqueOption.Automatic;
        public float maxDrawDistance = 1000f;
        public DBufferSettings dBufferSettings;
        public DecalScreenSpaceSettings screenSpaceSettings;
    }

    internal class SharedDecalEntityManager : System.IDisposable
    {
        private DecalEntityManager m_DecalEntityManager;
        private int m_ReferenceCounter;

        public DecalEntityManager Get()
        {
            if (m_DecalEntityManager == null)
            {
                Assert.AreEqual(m_ReferenceCounter, 0);

                m_DecalEntityManager = new DecalEntityManager();

                var decalProjectors = GameObject.FindObjectsOfType<DecalProjector>();
                foreach (var decalProjector in decalProjectors)
                {
                    if (!decalProjector.isActiveAndEnabled || m_DecalEntityManager.IsValid(decalProjector.decalEntity))
                        continue;
                    decalProjector.decalEntity = m_DecalEntityManager.CreateDecalEntity(decalProjector);
                }

                DecalProjector.onDecalAdd += OnDecalAdd;
                DecalProjector.onDecalRemove += OnDecalRemove;
                DecalProjector.onDecalPropertyChange += OnDecalPropertyChange;
                DecalProjector.onDecalMaterialChange += OnDecalMaterialChange;
            }

            m_ReferenceCounter++;

            return m_DecalEntityManager;
        }

        public void Release(DecalEntityManager decalEntityManager)
        {
            if (m_ReferenceCounter == 0)
                return;

            m_ReferenceCounter--;

            if (m_ReferenceCounter == 0)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            m_DecalEntityManager.Dispose();
            m_DecalEntityManager = null;
            m_ReferenceCounter = 0;

            DecalProjector.onDecalAdd -= OnDecalAdd;
            DecalProjector.onDecalRemove -= OnDecalRemove;
            DecalProjector.onDecalPropertyChange -= OnDecalPropertyChange;
            DecalProjector.onDecalMaterialChange -= OnDecalMaterialChange;
        }

        private void OnDecalAdd(DecalProjector decalProjector)
        {
            if (!m_DecalEntityManager.IsValid(decalProjector.decalEntity))
                decalProjector.decalEntity = m_DecalEntityManager.CreateDecalEntity(decalProjector);
        }

        private void OnDecalRemove(DecalProjector decalProjector)
        {
            m_DecalEntityManager.DestroyDecalEntity(decalProjector.decalEntity);
        }

        private void OnDecalPropertyChange(DecalProjector decalProjector)
        {
            if (m_DecalEntityManager.IsValid(decalProjector.decalEntity))
                m_DecalEntityManager.UpdateDecalEntityData(decalProjector.decalEntity, decalProjector);
        }

        private void OnDecalMaterialChange(DecalProjector decalProjector)
        {
            // Decal will end up in new chunk after material change
            OnDecalRemove(decalProjector);
            OnDecalAdd(decalProjector);
        }
    }

    [DisallowMultipleRendererFeature("Decal")]
    [Tooltip("With this Renderer Feature, Unity can project specific Materials (decals) onto other objects in the Scene.")]
    internal class DecalRendererFeature : ScriptableRendererFeature
    {
        private static SharedDecalEntityManager sharedDecalEntityManager { get; } = new SharedDecalEntityManager();

        [SerializeField]
        private DecalSettings m_Settings = new DecalSettings();

        [SerializeField]
        [HideInInspector]
        [Reload("Shaders/Utils/CopyDepth.shader")]
        private Shader m_CopyDepthPS;

        [SerializeField]
        [HideInInspector]
        [Reload("Runtime/Decal/DBuffer/DBufferClear.shader")]
        private Shader m_DBufferClear;

        private DecalTechnique m_Technique = DecalTechnique.Invalid;
        private DBufferSettings m_DBufferSettings;
        private DecalScreenSpaceSettings m_ScreenSpaceSettings;
        private bool m_RecreateSystems;

        private CopyDepthPass m_CopyDepthPass;
        private DecalPreviewPass m_DecalPreviewPass;
        private Material m_CopyDepthMaterial;

        // Entities
        private DecalEntityManager m_DecalEntityManager;
        private DecalUpdateCachedSystem m_DecalUpdateCachedSystem;
        private DecalUpdateCullingGroupSystem m_DecalUpdateCullingGroupSystem;
        private DecalUpdateCulledSystem m_DecalUpdateCulledSystem;
        private DecalCreateDrawCallSystem m_DecalCreateDrawCallSystem;
        private DecalDrawErrorSystem m_DrawErrorSystem;

        // DBuffer
        private DBufferRenderPass m_DBufferRenderPass;
        private DecalForwardEmissivePass m_ForwardEmissivePass;
        private DecalDrawDBufferSystem m_DecalDrawDBufferSystem;
        private DecalDrawFowardEmissiveSystem m_DecalDrawForwardEmissiveSystem;
        private Material m_DBufferClearMaterial;

        // Screen Space
        private DecalScreenSpaceRenderPass m_ScreenSpaceDecalRenderPass;
        private DecalDrawScreenSpaceSystem m_DecalDrawScreenSpaceSystem;
        private DecalSkipCulledSystem m_DecalSkipCulledSystem;

        // GBuffer
        private DecalGBufferRenderPass m_GBufferRenderPass;
        private DecalDrawGBufferSystem m_DrawGBufferSystem;
        private DeferredLights m_DeferredLights;

        internal bool intermediateRendering => m_Technique == DecalTechnique.DBuffer;
        internal static bool isGLDevice =>    SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                                              || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3
                                              || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;

        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif
            m_DecalPreviewPass = new DecalPreviewPass();
            m_RecreateSystems = true;
        }

        internal DBufferSettings GetDBufferSettings()
        {
            if (m_Settings.technique == DecalTechniqueOption.Automatic)
            {
                return new DBufferSettings() { surfaceData = DecalSurfaceData.AlbedoNormalMAOS };
            }
            else
            {
                return m_Settings.dBufferSettings;
            }
        }

        internal DecalScreenSpaceSettings GetScreenSpaceSettings()
        {
            if (m_Settings.technique == DecalTechniqueOption.Automatic)
            {
                return new DecalScreenSpaceSettings()
                {
                    normalBlend = DecalNormalBlend.Low,
                    useGBuffer = false,
                };
            }
            else
            {
                return m_Settings.screenSpaceSettings;
            }
        }

        internal DecalTechnique GetTechnique(ScriptableRendererData renderer)
        {
            var universalRenderer = renderer as UniversalRendererData;
            if (universalRenderer == null)
            {
                Debug.LogError("Only universal renderer supports Decal renderer feature.");
                return DecalTechnique.Invalid;
            }

            bool isDeferred = universalRenderer.renderingMode == RenderingMode.Deferred;
            return GetTechnique(isDeferred);
        }

        internal DecalTechnique GetTechnique(ScriptableRenderer renderer)
        {
            var universalRenderer = renderer as UniversalRenderer;
            if (universalRenderer == null)
            {
                Debug.LogError("Only universal renderer supports Decal renderer feature.");
                return DecalTechnique.Invalid;
            }

            bool isDeferred = universalRenderer.actualRenderingMode == RenderingMode.Deferred;
            return GetTechnique(isDeferred);
        }

        internal DecalTechnique GetTechnique(bool isDeferred, bool checkForInvalidTechniques = true)
        {
            DecalTechnique technique = DecalTechnique.Invalid;
            switch (m_Settings.technique)
            {
                case DecalTechniqueOption.Automatic:
                    if (IsAutomaticDBuffer())
                        technique = DecalTechnique.DBuffer;
                    else
                        technique = DecalTechnique.ScreenSpace;
                    break;
                case DecalTechniqueOption.ScreenSpace:
                    if (m_Settings.screenSpaceSettings.useGBuffer && isDeferred)
                        technique = DecalTechnique.GBuffer;
                    else
                        technique = DecalTechnique.ScreenSpace;
                    break;
                case DecalTechniqueOption.DBuffer:
                    technique = DecalTechnique.DBuffer;
                    break;
            }

            // In some cases the desired technique is wanted, even if not supported.
            // For example when building the player, so the variant can be included
            if (!checkForInvalidTechniques)
                return technique;

            // Check if the technique is valid
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
            {
                #if !UNITY_INCLUDE_TESTS
                Debug.LogError("Decals are not supported with OpenGLES2.");
                #endif
                return DecalTechnique.Invalid;
            }

            bool mrt4 = SystemInfo.supportedRenderTargetCount >= 4;
            if (technique == DecalTechnique.DBuffer && !mrt4)
            {
                Debug.LogError("Decal DBuffer technique requires MRT4 support.");
                return DecalTechnique.Invalid;
            }

            if (technique == DecalTechnique.GBuffer && !mrt4)
            {
                Debug.LogError("Decal useGBuffer option requires MRT4 support.");
                return DecalTechnique.Invalid;
            }

            return technique;
        }

        private bool IsAutomaticDBuffer()
        {
            // As WebGL uses gles here we should not use DBuffer
#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup == UnityEditor.BuildTargetGroup.WebGL)
                return false;
#else
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return false;
#endif
            return !GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.SHADER_API_MOBILE);
        }

        private bool RecreateSystemsIfNeeded(ScriptableRenderer renderer, in CameraData cameraData)
        {
            if (!m_RecreateSystems)
                return true;

            m_Technique = GetTechnique(renderer);
            if (m_Technique == DecalTechnique.Invalid)
                return false;

            m_DBufferSettings = GetDBufferSettings();
            m_ScreenSpaceSettings = GetScreenSpaceSettings();

            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(m_CopyDepthPS);
            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingPrePasses, m_CopyDepthMaterial);

            m_DBufferClearMaterial = CoreUtils.CreateEngineMaterial(m_DBufferClear);

            if (m_DecalEntityManager == null)
            {
                m_DecalEntityManager = sharedDecalEntityManager.Get();
            }

            m_DecalUpdateCachedSystem = new DecalUpdateCachedSystem(m_DecalEntityManager);
            m_DecalUpdateCulledSystem = new DecalUpdateCulledSystem(m_DecalEntityManager);
            m_DecalCreateDrawCallSystem = new DecalCreateDrawCallSystem(m_DecalEntityManager, m_Settings.maxDrawDistance);

            if (intermediateRendering)
            {
                m_DecalUpdateCullingGroupSystem = new DecalUpdateCullingGroupSystem(m_DecalEntityManager, m_Settings.maxDrawDistance);
            }
            else
            {
                m_DecalSkipCulledSystem = new DecalSkipCulledSystem(m_DecalEntityManager);
            }

            m_DrawErrorSystem = new DecalDrawErrorSystem(m_DecalEntityManager, m_Technique);

            var universalRenderer = renderer as UniversalRenderer;
            Assert.IsNotNull(universalRenderer);

            switch (m_Technique)
            {
                case DecalTechnique.ScreenSpace:
                    m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, m_DBufferClearMaterial);
                    m_DecalDrawScreenSpaceSystem = new DecalDrawScreenSpaceSystem(m_DecalEntityManager);
                    m_ScreenSpaceDecalRenderPass = new DecalScreenSpaceRenderPass(m_ScreenSpaceSettings, intermediateRendering ? m_DecalDrawScreenSpaceSystem : null);
                    break;

                case DecalTechnique.GBuffer:

                    m_DeferredLights = universalRenderer.deferredLights;

                    m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, m_DBufferClearMaterial);
                    m_DrawGBufferSystem = new DecalDrawGBufferSystem(m_DecalEntityManager);
                    m_GBufferRenderPass = new DecalGBufferRenderPass(m_ScreenSpaceSettings, intermediateRendering ? m_DrawGBufferSystem : null);
                    break;

                case DecalTechnique.DBuffer:
                    m_DecalDrawDBufferSystem = new DecalDrawDBufferSystem(m_DecalEntityManager);
                    m_DBufferRenderPass = new DBufferRenderPass(m_DBufferClearMaterial, m_DBufferSettings, m_DecalDrawDBufferSystem);

                    m_DecalDrawForwardEmissiveSystem = new DecalDrawFowardEmissiveSystem(m_DecalEntityManager);
                    m_ForwardEmissivePass = new DecalForwardEmissivePass(m_DecalDrawForwardEmissiveSystem);

                    if (universalRenderer.actualRenderingMode == RenderingMode.Deferred)
                    {
                        m_DBufferRenderPass.deferredLights = universalRenderer.deferredLights;
                        m_DBufferRenderPass.deferredLights.DisableFramebufferFetchInput();
                    }
                    break;
            }

            m_RecreateSystems = false;
            return true;
        }

        public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            if (cameraData.cameraType == CameraType.Preview)
                return;

            bool isValid = RecreateSystemsIfNeeded(renderer, cameraData);
            if (!isValid)
                return;

            ChangeAdaptivePerformanceDrawDistances();

            m_DecalEntityManager.Update();


            m_DecalUpdateCachedSystem.Execute();

            if (intermediateRendering)
            {
                m_DecalUpdateCullingGroupSystem.Execute(cameraData.camera);
            }
            else
            {
                m_DecalSkipCulledSystem.Execute(cameraData.camera);
                m_DecalCreateDrawCallSystem.Execute();

                if (m_Technique == DecalTechnique.ScreenSpace)
                {
                    m_DecalDrawScreenSpaceSystem.Execute(cameraData);
                }
                else if (m_Technique == DecalTechnique.GBuffer)
                {
                    m_DrawGBufferSystem.Execute(cameraData);
                }
            }

            m_DrawErrorSystem.Execute(cameraData);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview)
            {
                renderer.EnqueuePass(m_DecalPreviewPass);
                return;
            }

            bool isValid = RecreateSystemsIfNeeded(renderer, renderingData.cameraData);
            if (!isValid)
                return;

            ChangeAdaptivePerformanceDrawDistances();

            if (intermediateRendering)
            {
                m_DecalUpdateCulledSystem.Execute();
                m_DecalCreateDrawCallSystem.Execute();
            }

            switch (m_Technique)
            {
                case DecalTechnique.ScreenSpace:
                    renderer.EnqueuePass(m_ScreenSpaceDecalRenderPass);
                    break;
                case DecalTechnique.GBuffer:
                    m_GBufferRenderPass.Setup(m_DeferredLights);
                    renderer.EnqueuePass(m_GBufferRenderPass);
                    break;
                case DecalTechnique.DBuffer:
                    var universalRenderer = renderer as UniversalRenderer;
                    if (universalRenderer.actualRenderingMode == RenderingMode.Deferred)
                    {
                        m_CopyDepthPass.Setup(
                            new RenderTargetHandle(m_DBufferRenderPass.cameraDepthAttachmentIndentifier),
                            new RenderTargetHandle(m_DBufferRenderPass.cameraDepthTextureIndentifier)
                        );
                    }
                    else
                    {
                        m_CopyDepthPass.Setup(
                            new RenderTargetHandle(m_DBufferRenderPass.cameraDepthTextureIndentifier),
                            new RenderTargetHandle(m_DBufferRenderPass.dBufferDepthIndentifier)
                        );
                        m_CopyDepthPass.MssaSamples = 1;
                    }

                    renderer.EnqueuePass(m_CopyDepthPass);
                    renderer.EnqueuePass(m_DBufferRenderPass);
                    renderer.EnqueuePass(m_ForwardEmissivePass);
                    break;
            }
        }

        internal override bool SupportsNativeRenderPass()
        {
            return m_Technique == DecalTechnique.GBuffer || m_Technique == DecalTechnique.ScreenSpace;
        }
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_CopyDepthMaterial);
            CoreUtils.Destroy(m_DBufferClearMaterial);

            if (m_DecalEntityManager != null)
            {
                m_DecalEntityManager = null;
                sharedDecalEntityManager.Release(m_DecalEntityManager);
            }
        }

        [Conditional("ADAPTIVE_PERFORMANCE_4_0_0_OR_NEWER")]
        private void ChangeAdaptivePerformanceDrawDistances()
        {
#if ADAPTIVE_PERFORMANCE_4_0_0_OR_NEWER
            if (UniversalRenderPipeline.asset.useAdaptivePerformance)
            {
                if (m_DecalCreateDrawCallSystem != null)
                {
                    m_DecalCreateDrawCallSystem.maxDrawDistance = AdaptivePerformance.AdaptivePerformanceRenderSettings.DecalsDrawDistance;
                }
                if (m_DecalUpdateCullingGroupSystem != null)
                {
                    m_DecalUpdateCullingGroupSystem.boundingDistance = AdaptivePerformance.AdaptivePerformanceRenderSettings.DecalsDrawDistance;
                }
            }
#endif
        }
    }
}
