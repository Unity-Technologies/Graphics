using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public enum DecalSurfaceData
    {
        Albedo,
        AlbedoNormal,
        AlbedoNormalMask,
    }

    public enum DecalTechnique
    {
        Invalid,
        DBuffer,
        ScreenSpace,
        GBuffer,
    }

    public enum DecalTechniqueOption
    {
        Automatic,
        DBuffer,
        ScreenSpace,
    }

    [System.Serializable]
    public class DBufferSettings
    {
        public DecalSurfaceData surfaceData = DecalSurfaceData.AlbedoNormalMask;
    }

    public enum DecalNormalBlend
    {
        Off,
        NormalLow,
        NormalMedium,
        NormalHigh,
    }

    [System.Serializable]
    public class DecalScreenSpaceSettings
    {
        public DecalNormalBlend blend = DecalNormalBlend.NormalLow;
        public bool useGBuffer = true;
        [HideInInspector]
        public bool supportAdditionalLights = true;
    }

    [System.Serializable]
    public class DecalSettings
    {
        public DecalTechniqueOption technique = DecalTechniqueOption.Automatic;
        public float maxDrawDistance = 1000;
        public DBufferSettings dBufferSettings;
        public DecalScreenSpaceSettings screenSpaceSettings;

        public DecalSettings CreateAutomatic()
        {
            bool mrt4 = SystemInfo.supportedRenderTargetCount >= 4;

            if (mrt4 && (SystemInfo.deviceType == DeviceType.Desktop || SystemInfo.deviceType == DeviceType.Console))
                return new DecalSettings()
                {
                    technique = DecalTechniqueOption.DBuffer,
                    maxDrawDistance = maxDrawDistance,
                    dBufferSettings = new DBufferSettings()
                    {
                        surfaceData = DecalSurfaceData.AlbedoNormalMask,
                    }
                };
            else
                return new DecalSettings()
                {
                    technique = DecalTechniqueOption.ScreenSpace,
                    maxDrawDistance = maxDrawDistance,
                    screenSpaceSettings = new DecalScreenSpaceSettings()
                    {
                        useGBuffer = mrt4,
                        blend = SystemInfo.deviceType == DeviceType.Handheld ? DecalNormalBlend.NormalLow : DecalNormalBlend.NormalMedium,
                    }
                };
        }
    }

    public class SharedDecalEntityManager : System.IDisposable
    {
        private DecalEntityManager m_DecalEntityManager;
        private int m_UseCounter;

        public DecalEntityManager Get()
        {
            if (m_DecalEntityManager == null)
            {
                Assertions.Assert.AreEqual(m_UseCounter, 0);

                m_DecalEntityManager = new DecalEntityManager();

                var decalProjectors = GameObject.FindObjectsOfType<DecalProjector>();
                foreach (var decalProjector in decalProjectors)
                {
                    if (m_DecalEntityManager.IsValid(decalProjector.decalEntity))
                        continue;
                    decalProjector.decalEntity = m_DecalEntityManager.CreateDecalEntity(decalProjector);
                }

                DecalProjector.onDecalAdd += OnDecalAdd;
                DecalProjector.onDecalRemove += OnDecalRemove;
                DecalProjector.onDecalPropertyChange += OnDecalPropertyChange;
                DecalProjector.onDecalMaterialChange += OnDecalMaterialChange;
            }

            m_UseCounter++;

            return m_DecalEntityManager;
        }

        public void Release(DecalEntityManager decalEntityManager)
        {
            if (m_UseCounter == 0)
                return;

            m_UseCounter--;

            if (m_UseCounter == 0)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            m_DecalEntityManager.Dispose();
            m_DecalEntityManager = null;
            m_UseCounter = 0;


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
    public class DecalRendererFeature : ScriptableRendererFeature
    {
        private static SharedDecalEntityManager sharedDecalEntityManager { get; } = new SharedDecalEntityManager();

        public DecalSettings settings = new DecalSettings(); // todo

        private DecalTechnique m_Technique = DecalTechnique.Invalid;
        private DBufferSettings m_DBufferSettings;
        private DecalScreenSpaceSettings m_ScreenSpaceSettings;

        private bool m_DirtySystems;

        [HideInInspector]
        [Reload("Shaders/Utils/CopyDepth.shader")]
        public Shader copyDepthPS;

        [HideInInspector]
        [Reload("Runtime/Decal/DBuffer/DBufferClear.shader")]
        public Shader dBufferClear;

        private CopyDepthPass m_CopyDepthPass;
        private DBufferRenderPass m_DBufferRenderPass;
        private DecalForwardEmissivePass m_ForwardEmissivePass;
        private DecalPreviewPass m_DecalPreviewPass;
        private NormalReconstructionSetupPass m_NormalReconstructionSetupPass;
        private DecalDrawErrorSystem m_DrawErrorSystem;

        // Entities
        private DecalEntityManager m_DecalEntityManager;
        private DecalUpdateCachedSystem m_DecalUpdateCachedSystem;
        private DecalUpdateCullingGroupSystem m_DecalUpdateCullingGroupSystem;
        private DecalUpdateCulledSystem m_DecalUpdateCulledSystem;
        private DecalCreateDrawCallSystem m_DecalCreateDrawCallSystem;

        // DBuffer
        private DecalDrawIntoDBufferSystem m_DecalDrawIntoDBufferSystem;
        private DecalDrawFowardEmissiveSystem m_DecalDrawForwardEmissiveSystem;

        // Screen Space
        private ScreenSpaceDecalRenderPass m_ScreenSpaceDecalRenderPass;
        private DecalDrawScreenSpaceSystem m_DecalDrawScreenSpaceSystem;
        private DecalSkipCulledSystem m_DecalSkipCulledSystem;

        // GBuffer
        private DecalGBufferRenderPass m_GBufferRenderPass;
        private DecalDrawGBufferSystem m_DrawGBufferSystem;

        internal bool intermmediateRendering => m_Technique == DecalTechnique.DBuffer;

        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif
            m_DecalPreviewPass = new DecalPreviewPass("Decal Preview Render");
            m_NormalReconstructionSetupPass = new NormalReconstructionSetupPass("Normal Reconstruction Setup");
            m_DirtySystems = true;
        }

        internal DBufferSettings GetDBufferSettings()
        {
            if (settings.technique == DecalTechniqueOption.Automatic)
            {
                return new DBufferSettings() { surfaceData = DecalSurfaceData.AlbedoNormalMask };
            }
            else
            {
                return settings.dBufferSettings;
            }
        }

        internal DecalScreenSpaceSettings GetScreenSpaceSettings()
        {
            if (settings.technique == DecalTechniqueOption.Automatic)
            {
                return new DecalScreenSpaceSettings()
                {
                    blend = DecalNormalBlend.NormalLow,
                    useGBuffer = false,
                };
            }
            else
            {
                return settings.screenSpaceSettings;
            }
        }

        internal DecalTechnique GetTechnique(ScriptableRenderer renderer)
        {
            var universalRenderer = renderer as UniversalRenderer;
            if (universalRenderer == null)
            {
                Debug.LogError("Only universal renderer supports decal renderer feature.");
                return DecalTechnique.Invalid;
            }

            DecalTechnique technique = DecalTechnique.Invalid;
            switch (settings.technique)
            {
                case DecalTechniqueOption.Automatic:
                    if (IsHandheld())
                        technique = DecalTechnique.ScreenSpace;
                    else
                        technique = DecalTechnique.DBuffer;
                    break;
                case DecalTechniqueOption.ScreenSpace:
                    bool isDeferred = universalRenderer.actualRenderingMode == RenderingMode.Deferred;
                    if (settings.screenSpaceSettings.useGBuffer && isDeferred)
                        technique = DecalTechnique.GBuffer;
                    else
                        technique = DecalTechnique.ScreenSpace;
                    break;
                case DecalTechniqueOption.DBuffer:
                    technique = DecalTechnique.DBuffer;
                    break;
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

        private bool IsHandheld()
        {
#if UNITY_EDITOR
            var selectedBuildTargetGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
            if (selectedBuildTargetGroup == UnityEditor.BuildTargetGroup.Android)
                return true;
            if (selectedBuildTargetGroup == UnityEditor.BuildTargetGroup.iOS)
                return true;
            if (selectedBuildTargetGroup == UnityEditor.BuildTargetGroup.Switch)
                return true;
#endif
            return SystemInfo.deviceType == DeviceType.Handheld;
        }

        private void RecreateSystemsIfNeeded(ScriptableRenderer renderer)
        {
            if (!m_DirtySystems)
                return;

            m_Technique = GetTechnique(renderer);
            m_DBufferSettings = GetDBufferSettings();
            m_ScreenSpaceSettings = GetScreenSpaceSettings();

            var copyDepthMaterial = CoreUtils.CreateEngineMaterial(copyDepthPS);
            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingPrePasses, copyDepthMaterial);

            var dBufferClearMaterial = CoreUtils.CreateEngineMaterial(dBufferClear);

            if (m_DecalEntityManager == null)
            {
                m_DecalEntityManager = sharedDecalEntityManager.Get();
            }

            m_DecalUpdateCachedSystem = new DecalUpdateCachedSystem(m_DecalEntityManager);
            m_DecalUpdateCulledSystem = new DecalUpdateCulledSystem(m_DecalEntityManager);
            m_DecalCreateDrawCallSystem = new DecalCreateDrawCallSystem(m_DecalEntityManager);

            if (intermmediateRendering)
            {
                m_DecalUpdateCullingGroupSystem = new DecalUpdateCullingGroupSystem(m_DecalEntityManager, settings.maxDrawDistance);
            }
            else
            {
                m_DecalSkipCulledSystem = new DecalSkipCulledSystem(m_DecalEntityManager);
            }

            m_DrawErrorSystem = new DecalDrawErrorSystem(m_DecalEntityManager, m_Technique);

            switch (m_Technique)
            {
                case DecalTechnique.ScreenSpace:
                    m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthMaterial);
                    m_DecalDrawScreenSpaceSystem = new DecalDrawScreenSpaceSystem(m_DecalEntityManager);
                    m_ScreenSpaceDecalRenderPass = new ScreenSpaceDecalRenderPass("Decal Screen Space Render", m_ScreenSpaceSettings, m_DecalDrawScreenSpaceSystem);
                    break;

                case DecalTechnique.GBuffer:
                    m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthMaterial);
                    m_DrawGBufferSystem = new DecalDrawGBufferSystem(m_DecalEntityManager);
                    m_GBufferRenderPass = new DecalGBufferRenderPass("Decal GBuffer Render", m_ScreenSpaceSettings, m_DrawGBufferSystem);
                    break;

                case DecalTechnique.DBuffer:
                    m_DecalDrawIntoDBufferSystem = new DecalDrawIntoDBufferSystem(m_DecalEntityManager);
                    m_DBufferRenderPass = new DBufferRenderPass("DBuffer Render", dBufferClearMaterial, m_DBufferSettings, m_DecalDrawIntoDBufferSystem);

                    m_DecalDrawForwardEmissiveSystem = new DecalDrawFowardEmissiveSystem(m_DecalEntityManager);
                    m_ForwardEmissivePass = new DecalForwardEmissivePass("Decal Forward Emissive Render", m_DecalDrawForwardEmissiveSystem);
                    break;
            }

            m_DirtySystems = false;
        }

        internal override void OnCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            if (cameraData.cameraType == CameraType.Preview)
                return;

            RecreateSystemsIfNeeded(renderer);

            m_DecalUpdateCachedSystem.Execute();

            if (intermmediateRendering)
            {
                m_DecalUpdateCullingGroupSystem.Execute(cameraData.camera);

                m_DecalEntityManager.Sort();
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

            renderer.EnqueuePass(m_NormalReconstructionSetupPass);

            RecreateSystemsIfNeeded(renderer);

            if (intermmediateRendering)
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
                    var universalRenderer = renderer as UniversalRenderer;
                    m_GBufferRenderPass.Setup(universalRenderer.deferredLights);
                    renderer.EnqueuePass(m_GBufferRenderPass);
                    break;
                case DecalTechnique.DBuffer:
                    m_CopyDepthPass.Setup(
                        new RenderTargetHandle(new RenderTargetIdentifier("_CameraDepthTexture")),
                        new RenderTargetHandle(new RenderTargetIdentifier("DBufferDepth"))
                    );
                    renderer.EnqueuePass(m_CopyDepthPass);
                    renderer.EnqueuePass(m_DBufferRenderPass);
                    renderer.EnqueuePass(m_ForwardEmissivePass);
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_DecalEntityManager != null)
            {
                m_DecalEntityManager = null;
                sharedDecalEntityManager.Release(m_DecalEntityManager);
            }
        }
    }
}
