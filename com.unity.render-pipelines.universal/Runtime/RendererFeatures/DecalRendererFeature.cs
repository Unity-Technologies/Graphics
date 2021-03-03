using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public enum DecalVersion
    {
        HDRP,
        NewDOD,
    }

    public class DecalRendererFeature : ScriptableRendererFeature
    {
        public DecalVersion decalVersion;

        [Reload("Shaders/Utils/CopyDepth.shader")]
        public Shader copyDepthPS;

        CopyDepthPass copyDepthPass;

        DBufferRenderPass renderObjectsPass;

        DecalSystem.CullRequest decalCullRequest;
        DecalSystem.CullResult decalCullResult;
        ProfilingSampler decalSystemCull;
        ProfilingSampler decalSystemCullEnd;

        DecalEntityManager m_DecalEntityManager;
        DecalUpdateCachedSystem m_DecalUpdateCachedSystem;
        DecalUpdateCullingGroupSystem m_DecalUpdateCullingGroupSystem;
        DecalUpdateCulledSystem m_DecalUpdateCulledSystem;
        DecalCreateDrawCallSystem m_DecalCreateDrawCallSystem;
        DecalDrawIntoDBufferSystem m_DecalDrawIntoDBufferSystem;

        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif
            var copyDepthMaterial = CoreUtils.CreateEngineMaterial(copyDepthPS);
            copyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingPrePasses, copyDepthMaterial);

            renderObjectsPass = new DBufferRenderPass("DBuffer Render", RenderPassEvent.BeforeRenderingPrePasses, DecalSystem.s_MaterialDecalPassNames);

            if (decalVersion == DecalVersion.HDRP)
            {
                decalSystemCull = new ProfilingSampler("V1.DecalSystem.BeginCull");
                decalSystemCullEnd = new ProfilingSampler("V1.DecalSystem.EndCull");
            }

            if (decalVersion == DecalVersion.NewDOD)
            {
                // This call will completely recreate decals, so try to call it rare as possible
                if (m_DecalEntityManager == null)
                {
                    m_DecalEntityManager = new DecalEntityManager();

                    var decalProjectors = GameObject.FindObjectsOfType<DecalProjector>();
                    foreach (var decalProjector in decalProjectors)
                    {
                        if (m_DecalEntityManager.IsValid(decalProjector.decalEntity))
                            continue;
                        decalProjector.decalEntity = m_DecalEntityManager.CreateDecalEntity(decalProjector);
                    }

                    DecalProjector.onDecalAdd += OnAddDecalProjector;
                    DecalProjector.onDecalRemove += OnRemoveDecalProjector;


                    Debug.Log("new DecalEntityManager");
                }

                m_DecalUpdateCachedSystem = new DecalUpdateCachedSystem(m_DecalEntityManager);
                m_DecalUpdateCullingGroupSystem = new DecalUpdateCullingGroupSystem(m_DecalEntityManager);
                m_DecalUpdateCulledSystem = new DecalUpdateCulledSystem(m_DecalEntityManager);
                m_DecalCreateDrawCallSystem = new DecalCreateDrawCallSystem(m_DecalEntityManager);
                m_DecalDrawIntoDBufferSystem = new DecalDrawIntoDBufferSystem(m_DecalEntityManager);
                renderObjectsPass.m_DecalDrawIntoDBufferSystem = m_DecalDrawIntoDBufferSystem;

                Debug.Log("new DecalSystems");
            }
        }

        private void OnAddDecalProjector(DecalProjector decalProjector)
        {
            if (!m_DecalEntityManager.IsValid(decalProjector.decalEntity))
                decalProjector.decalEntity = m_DecalEntityManager.CreateDecalEntity(decalProjector);
        }

        private void OnRemoveDecalProjector(DecalProjector decalProjector)
        {
            m_DecalEntityManager.DestroyDecalEntity(decalProjector.decalEntity);
        }

        internal override void OnCull(in CameraData cameraData)
        {
            if (decalVersion == DecalVersion.HDRP)
            {
                using (new ProfilingScope(null, decalSystemCull))
                {
                    // decal system needs to be updated with current camera, it needs it to set up culling and light list generation parameters
                    decalCullRequest = GenericPool<DecalSystem.CullRequest>.Get();
                    DecalSystem.instance.CurrentCamera = cameraData.camera;
                    DecalSystem.instance.BeginCull(decalCullRequest);
                }
            }

            if (decalVersion == DecalVersion.NewDOD)
            {
                m_DecalUpdateCachedSystem.Execute();
                m_DecalUpdateCullingGroupSystem.Execute(cameraData.camera);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (decalVersion == DecalVersion.HDRP)
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
                }
            }

            if (decalVersion == DecalVersion.NewDOD)
            {
                m_DecalUpdateCulledSystem.Execute();
                m_DecalCreateDrawCallSystem.Execute();
            }

            copyDepthPass.Setup(
                new RenderTargetHandle(new RenderTargetIdentifier("_CameraDepthTexture")),
                new RenderTargetHandle(new RenderTargetIdentifier("DBufferDepth"))
            );
            renderer.EnqueuePass(copyDepthPass);
            renderer.EnqueuePass(renderObjectsPass);
        }

        protected override void Dispose(bool disposing)
        {
            m_DecalEntityManager.Dispose();
            m_DecalEntityManager = null;
            DecalProjector.onDecalAdd -= OnAddDecalProjector;
            DecalProjector.onDecalRemove -= OnRemoveDecalProjector;

            //DecalEntityManager.active = null;
        }
    }
}
