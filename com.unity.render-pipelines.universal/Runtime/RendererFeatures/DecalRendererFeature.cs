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
        Automatic,
        DBuffer,
        ScreenSpace,
        //DBUFFER_HDRP_PORT,
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
        public bool supportAdditionalLights = false;
    }

    [System.Serializable]
    public class DecalSettings
    {
        public DecalTechnique technique = DecalTechnique.Automatic;
        public float maxDrawDistance = 1000;
        public DBufferSettings dBufferSettings;
        public DecalScreenSpaceSettings screenSpaceSettings;

        public DecalSettings CreateAutomatic()
        {
            bool mrt4 = SystemInfo.supportedRenderTargetCount >= 4;

            if (mrt4 && (SystemInfo.deviceType == DeviceType.Desktop || SystemInfo.deviceType == DeviceType.Console))
                return new DecalSettings()
                {
                    technique = DecalTechnique.DBuffer,
                    maxDrawDistance = maxDrawDistance,
                    dBufferSettings = new DBufferSettings()
                    {
                        surfaceData = DecalSurfaceData.AlbedoNormalMask,
                    }
                };
            else
                return new DecalSettings()
                {
                    technique = DecalTechnique.ScreenSpace,
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
        private static SharedDecalEntityManager s_SharedDecalEntityManager;
        private static SharedDecalEntityManager sharedDecalEntityManager
        {
            get
            {
                if (s_SharedDecalEntityManager == null)
                    s_SharedDecalEntityManager = new SharedDecalEntityManager();
                return s_SharedDecalEntityManager;
            }
        }

        public DecalSettings settings = new DecalSettings(); // todo
        private DecalSettings m_ActualSettings;

        private bool m_TechniqueValid;
        private bool m_DirtySystems;

        [HideInInspector]
        [Reload("Shaders/Utils/CopyDepth.shader")]
        public Shader copyDepthPS;

        [HideInInspector]
        [Reload("Runtime/Decal/DBufferClear.shader")]
        public Shader dBufferClear;

        private CopyDepthPass m_CopyDepthPass;
        private DBufferRenderPass m_DBufferRenderPass;
        private DecalForwardEmissivePass m_ForwardEmissivePass;
        private DecalPreviewPass m_DecalPreviewPass;
        private NormalReconstructionSetupPass m_NormalReconstructionSetupPass;

        // TODO: Remove
        /*DecalSystem.CullRequest decalCullRequest;
        DecalSystem.CullResult decalCullResult;
        ProfilingSampler decalSystemCull;
        ProfilingSampler decalSystemCullEnd;*/

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

        private DecalSettings actualSettings { get => m_ActualSettings; }
        public DecalTechnique technique { get => m_ActualSettings.technique; }
        public bool intermmediateRendering { get => !(technique == DecalTechnique.ScreenSpace && settings.screenSpaceSettings.supportAdditionalLights); }

        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif
            m_DecalPreviewPass = new DecalPreviewPass("Decal Preview Render");

            m_NormalReconstructionSetupPass = new NormalReconstructionSetupPass("Normal Reconstruction Setup");

            if (settings.technique == DecalTechnique.Automatic)
            {
                m_ActualSettings = settings.CreateAutomatic();
            }
            else
            {
                m_ActualSettings = settings;

                bool mrt4 = SystemInfo.supportedRenderTargetCount >= 4;
                if (technique == DecalTechnique.DBuffer && !mrt4)
                {
                    Debug.LogError("Decal DBuffer technique requires MRT4 support.");
                    m_TechniqueValid = false;
                    return;
                }

                if (technique == DecalTechnique.ScreenSpace && actualSettings.screenSpaceSettings.useGBuffer && !mrt4)
                {
                    Debug.LogError("Decal useGBuffer option requires MRT4 support.");
                    m_TechniqueValid = false;
                    return;
                }
            }

            m_DirtySystems = true;
            m_TechniqueValid = true;
        }

        private void RecreateSystemsIfNeeded()
        {
            if (!m_DirtySystems)
                return;

            var copyDepthMaterial = CoreUtils.CreateEngineMaterial(copyDepthPS);
            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingPrePasses, copyDepthMaterial);

            var dBufferClearMaterial = CoreUtils.CreateEngineMaterial(dBufferClear);

            /*if (actualSettings.technique == DecalTechnique.DBUFFER_HDRP_PORT)
            {
                decalSystemCull = new ProfilingSampler("V1.DecalSystem.BeginCull");
                decalSystemCullEnd = new ProfilingSampler("V1.DecalSystem.EndCull");

                m_DBufferRenderPass = new DBufferRenderPass("DBuffer Render", dBufferClearMaterial, actualSettings.dBufferSettings, null);
            }*/

            if (technique == DecalTechnique.DBuffer || technique == DecalTechnique.ScreenSpace)
            {
                if (m_DecalEntityManager == null)
                {
                    m_DecalEntityManager = sharedDecalEntityManager.Get();
                }

                m_DecalUpdateCachedSystem = new DecalUpdateCachedSystem(m_DecalEntityManager);
                m_DecalUpdateCullingGroupSystem = new DecalUpdateCullingGroupSystem(m_DecalEntityManager, actualSettings.maxDrawDistance);
                m_DecalUpdateCulledSystem = new DecalUpdateCulledSystem(m_DecalEntityManager);
                m_DecalCreateDrawCallSystem = new DecalCreateDrawCallSystem(m_DecalEntityManager);

                if (technique == DecalTechnique.ScreenSpace)
                {
                    m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthMaterial);

                    m_DrawGBufferSystem = new DecalDrawGBufferSystem(m_DecalEntityManager);
                    m_GBufferRenderPass = new DecalGBufferRenderPass("Decal GBuffer Render", actualSettings.screenSpaceSettings, m_DrawGBufferSystem);

                    m_DecalDrawScreenSpaceSystem = new DecalDrawScreenSpaceSystem(m_DecalEntityManager);
                    m_ScreenSpaceDecalRenderPass = new ScreenSpaceDecalRenderPass("Decal Screen Space Render", actualSettings.screenSpaceSettings, m_DecalDrawScreenSpaceSystem);

                    m_DecalSkipCulledSystem = new DecalSkipCulledSystem(m_DecalEntityManager);
                }
                else
                {
                    m_DecalDrawIntoDBufferSystem = new DecalDrawIntoDBufferSystem(m_DecalEntityManager);
                    m_DBufferRenderPass = new DBufferRenderPass("DBuffer Render", dBufferClearMaterial, actualSettings.dBufferSettings, m_DecalDrawIntoDBufferSystem);

                    m_DecalDrawForwardEmissiveSystem = new DecalDrawFowardEmissiveSystem(m_DecalEntityManager);
                    m_ForwardEmissivePass = new DecalForwardEmissivePass("Decal Forward Emissive Render", m_DecalDrawForwardEmissiveSystem);
                }
            }

            m_DirtySystems = false;
        }

        internal override void OnCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            if (!m_TechniqueValid)
                return;

            if (cameraData.cameraType == CameraType.Preview)
                return;

            /*if (technique == DecalTechnique.DBUFFER_HDRP_PORT)
            {
                using (new ProfilingScope(null, decalSystemCull))
                {
                    // decal system needs to be updated with current camera, it needs it to set up culling and light list generation parameters
                    decalCullRequest = GenericPool<DecalSystem.CullRequest>.Get();
                    DecalSystem.instance.CurrentCamera = cameraData.camera;
                    DecalSystem.instance.BeginCull(decalCullRequest);
                }
            }*/

            if (technique == DecalTechnique.DBuffer || technique == DecalTechnique.ScreenSpace)
            {
                RecreateSystemsIfNeeded();

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
                    //m_DecalDrawRendererSystem.Execute(cameraData);

                    if (technique == DecalTechnique.ScreenSpace)
                    {
                        var universalRenderer = renderer as UniversalRenderer;
                        bool deferred = universalRenderer?.actualRenderingMode == RenderingMode.Deferred;
                        if (actualSettings.screenSpaceSettings.useGBuffer && deferred)
                        {
                            m_DrawGBufferSystem.Execute(cameraData);
                        }
                        else
                        {
                            m_DecalDrawScreenSpaceSystem.Execute(cameraData);
                        }
                    }
                }
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!m_TechniqueValid)
                return;

            if (renderingData.cameraData.cameraType == CameraType.Preview)
            {
                renderer.EnqueuePass(m_DecalPreviewPass);
                return;
            }

            renderer.EnqueuePass(m_NormalReconstructionSetupPass);

            /*if (technique == DecalTechnique.DBUFFER_HDRP_PORT)
            {
                using (new ProfilingScope(null, decalSystemCullEnd))
                {
                    decalCullResult = GenericPool<DecalSystem.CullResult>.Get();
                    DecalSystem.instance.EndCull(decalCullRequest, decalCullResult);

                    if (decalCullRequest != null)
                    {
                        decalCullRequest.Clear();
                        GenericPool<DecalSystem.CullRequest>.Release(decalCullRequest);
                    }

                    // TODO: update singleton with DecalCullResults
                    DecalSystem.instance.CurrentCamera = renderingData.cameraData.camera; // Singletons are extremely dangerous...
                    DecalSystem.instance.LoadCullResults(decalCullResult);
                    DecalSystem.instance.UpdateCachedMaterialData(); // textures, alpha or fade distances could've changed
                    DecalSystem.instance.CreateDrawData();          // prepare data is separate from draw
                                                                    //DecalSystem.instance.UpdateTextureAtlas(cmd);   // as this is only used for transparent pass, would've been nice not to have to do this if no transparent renderers are visible, needs to happen after CreateDrawData

                    if (decalCullResult != null)
                    {
                        decalCullResult.Clear();
                        GenericPool<DecalSystem.CullResult>.Release(decalCullResult);
                    }


                    m_CopyDepthPass.Setup(
                        new RenderTargetHandle(new RenderTargetIdentifier("_CameraDepthTexture")),
                        new RenderTargetHandle(new RenderTargetIdentifier("DBufferDepth"))
                    );
                    m_CopyDepthPass.AllocateRT = false;
                    renderer.EnqueuePass(m_CopyDepthPass);
                    renderer.EnqueuePass(m_DBufferRenderPass);
                }
            }*/

            if (technique == DecalTechnique.DBuffer || technique == DecalTechnique.ScreenSpace)
            {
                RecreateSystemsIfNeeded();

                if (intermmediateRendering)
                {
                    m_DecalUpdateCulledSystem.Execute();
                    m_DecalCreateDrawCallSystem.Execute();
                }
                else
                {
                    //m_DecalDrawRendererSystem.Execute(renderingData.cameraData);
                }

                if (technique == DecalTechnique.ScreenSpace)
                {
                    var universalRenderer = renderer as UniversalRenderer;
                    bool deferred = universalRenderer?.actualRenderingMode == RenderingMode.Deferred;
                    if (actualSettings.screenSpaceSettings.useGBuffer && deferred)
                    {
                        m_GBufferRenderPass.Setup(universalRenderer.deferredLights);
                        renderer.EnqueuePass(m_GBufferRenderPass);
                    }
                    else
                    {
                        renderer.EnqueuePass(m_ScreenSpaceDecalRenderPass);
                    }
                }
                else
                {
                    m_CopyDepthPass.Setup(
                        new RenderTargetHandle(new RenderTargetIdentifier("_CameraDepthTexture")),
                        new RenderTargetHandle(new RenderTargetIdentifier("DBufferDepth"))
                    );
                    renderer.EnqueuePass(m_CopyDepthPass);
                    renderer.EnqueuePass(m_DBufferRenderPass);
                    renderer.EnqueuePass(m_ForwardEmissivePass);
                }
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
