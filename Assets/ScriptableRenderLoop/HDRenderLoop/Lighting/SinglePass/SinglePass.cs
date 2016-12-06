using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public class SinglePass : LightLoop
    {
        string GetKeyword()
        {
            return "LIGHTLOOP_SINGLE_PASS";
        }

        public const int k_MaxDirectionalLightsOnSCreen = 3;
        public const int k_MaxPunctualLightsOnSCreen = 512;
        public const int k_MaxAreaLightsOnSCreen = 128;
        public const int k_MaxEnvLightsOnSCreen = 64;
        public const int k_MaxShadowOnScreen = 16;
        public const int k_MaxCascadeCount = 4; //Should be not less than m_Settings.directionalLightCascadeCount;

        Material m_DeferredMaterial = null;

        public override void Rebuild()
        {
            base.Rebuild();

            m_DeferredMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/Deferred");
            m_DeferredMaterial.EnableKeyword(GetKeyword());
        }

        public override void Cleanup()
        {
            base.Cleanup();

            Utilities.Destroy(m_DeferredMaterial);
        }

        public override void PrepareLightsForGPU(CullResults cullResults, Camera camera)
        {
        }

        public override void PushGlobalParams(Camera camera, RenderLoop loop)
        {
            base.PushGlobalParams();
        }

        public override void RenderDeferredLighting(Camera camera, RenderLoop renderLoop, RenderTargetIdentifier cameraColorBufferRT)
        {
            var invViewProj = Utilities.GetViewProjectionMatrix(camera).inverse;
            m_DeferredMaterial.SetMatrix("_InvViewProjMatrix", invViewProj);

            var screenSize = Utilities.ComputeScreenSize(camera);
            m_DeferredMaterial.SetVector("_ScreenSize", screenSize);

            m_DeferredMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m_DeferredMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);

            // m_gbufferManager.BindBuffers(m_DeferredMaterial);
            // TODO: Bind depth textures

            using (new Utilities.ProfilingSample("SinglePass - Deferred Lighting Pass", renderLoop))
            {
                var cmd = new CommandBuffer { name = "" };
                cmd.Blit(null, cameraColorBufferRT, m_DeferredMaterial, 0);
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }
        }
    }
}
