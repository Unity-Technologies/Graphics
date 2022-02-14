
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal enum PostProInjectionPoint
    {
        AfterOpaqueAndSky,
        AfterPostProcess
    }

    // TODO - needs to be changed to matrix
    internal enum PostProRequirements
    {
        Various
    }


    [Tooltip("Custom Post Processing renderer feature lets you easily assign and use custom post processing effects")]
    internal class CustomPostProRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private PostProInjectionPoint injectionPoint = PostProInjectionPoint.AfterOpaqueAndSky;
        [SerializeField]
        private PostProRequirements requirements = PostProRequirements.Various;
        [SerializeField]
        private Material material; // TODO - make material or shader option here.
        [SerializeField]
        private int passIndex;

        private CustomPostProPass m_CustomPostProPass;

        public override void Create()
        {
            if (m_CustomPostProPass == null)
            {
                m_CustomPostProPass = new CustomPostProPass();
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null)
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }

            m_CustomPostProPass.Setup(material, passIndex);
            renderer.EnqueuePass(m_CustomPostProPass);
        }

        private class CustomPostProPass : ScriptableRenderPass
        {
            // TODO - Post process pass could potentially be reused for medium code path
            private Material m_UsedMaterial;
            private int m_PassIndex;

            public void Setup(Material mat, int passIndex)
            {
                m_UsedMaterial = mat;
                m_PassIndex = passIndex;
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }

            public enum ProfilerMarkers
            {
                PostPro_SRP_Execute
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_UsedMaterial == null) return;


                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, ProfilingSampler.Get(ProfilerMarkers.PostPro_SRP_Execute)))
                {
                    ref ScriptableRenderer renderer = ref renderingData.cameraData.renderer;

                    RTHandle source = renderer.cameraColorTargetHandle;
                    RTHandle dest = renderer.GetCameraColorFrontBuffer(cmd);

                    m_UsedMaterial.SetFloat("_Intensity", 0.5f);
                    m_UsedMaterial.SetTexture("_InputTexture",
                        renderer.cameraColorTargetHandle);

                    cmd.SetRenderTarget(source);
                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                    cmd.SetViewport(renderingData.cameraData.pixelRect);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_UsedMaterial, 0, m_PassIndex);

                    CoreUtils.Swap(ref source, ref dest);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }
        }
    }
}
