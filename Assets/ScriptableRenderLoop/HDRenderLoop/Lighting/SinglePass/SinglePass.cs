using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    namespace SinglePass
    {
        //-----------------------------------------------------------------------------
        // structure definition
        //-----------------------------------------------------------------------------

        public class LightLoop
        {
            string GetKeyword()
            {
                return "LIGHTLOOP_SINGLE_PASS";
            }

            // Static keyword is required here else we get a "DestroyBuffer can only be call in main thread"
            static ComputeBuffer s_DirectionalLights = null;
            static ComputeBuffer s_PunctualLightList = null;
            static ComputeBuffer s_EnvLightList = null;
            static ComputeBuffer s_AreaLightList = null;
            static ComputeBuffer s_PunctualShadowList = null;
            static ComputeBuffer s_DirectionalShadowList = null;

            Material m_DeferredMaterial = null;

            public void Rebuild()
            {
                s_DirectionalLights = new ComputeBuffer(HDRenderLoop.k_MaxDirectionalLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightData)));
                s_DirectionalShadowList = new ComputeBuffer(HDRenderLoop.k_MaxCascadeCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalShadowData)));
                s_PunctualLightList = new ComputeBuffer(HDRenderLoop.k_MaxPunctualLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
                s_AreaLightList = new ComputeBuffer(HDRenderLoop.k_MaxAreaLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
                s_EnvLightList = new ComputeBuffer(HDRenderLoop.k_MaxEnvLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
                s_PunctualShadowList = new ComputeBuffer(HDRenderLoop.k_MaxShadowOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualShadowData)));

                m_DeferredMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderLoop/Deferred");
                m_DeferredMaterial.EnableKeyword("LIGHTLOOP_SINGLE_PASS");
            }

            public void Cleanup()
            {
                Utilities.SafeRelease(s_DirectionalLights);
                Utilities.SafeRelease(s_DirectionalShadowList);
                Utilities.SafeRelease(s_PunctualLightList);
                Utilities.SafeRelease(s_AreaLightList);
                Utilities.SafeRelease(s_EnvLightList);
                Utilities.SafeRelease(s_PunctualShadowList);

                Utilities.Destroy(m_DeferredMaterial);
            }

            public void PrepareLightsForGPU(CullResults cullResults, Camera camera, HDRenderLoop.LightList lightList) {}

            public void PushGlobalParams(Camera camera, RenderLoop loop, HDRenderLoop.LightList lightList)
            {
                s_DirectionalLights.SetData(lightList.directionalLights.ToArray());
                s_DirectionalShadowList.SetData(lightList.directionalShadows.ToArray());
                s_PunctualLightList.SetData(lightList.punctualLights.ToArray());
                s_AreaLightList.SetData(lightList.areaLights.ToArray());
                s_EnvLightList.SetData(lightList.envLights.ToArray());
                s_PunctualShadowList.SetData(lightList.punctualShadows.ToArray());

                Shader.SetGlobalBuffer("_DirectionalLightList", s_DirectionalLights);
                Shader.SetGlobalInt("_DirectionalLightCount", lightList.directionalLights.Count);
                Shader.SetGlobalBuffer("_DirectionalShadowList", s_DirectionalShadowList);
                Shader.SetGlobalBuffer("_PunctualLightList", s_PunctualLightList);
                Shader.SetGlobalInt("_PunctualLightCount", lightList.punctualLights.Count);
                Shader.SetGlobalBuffer("_AreaLightList", s_AreaLightList);
                Shader.SetGlobalInt("_AreaLightCount", lightList.areaLights.Count);  
                Shader.SetGlobalBuffer("_PunctualShadowList", s_PunctualShadowList);
                Shader.SetGlobalBuffer("_EnvLightList", s_EnvLightList);
                Shader.SetGlobalInt("_EnvLightCount", lightList.envLights.Count);

                Shader.SetGlobalVectorArray("_DirShadowSplitSpheres", lightList.directionalShadowSplitSphereSqr);      
            }

            public void RenderDeferredLighting(Camera camera, RenderLoop renderLoop, RenderTargetIdentifier cameraColorBufferRT)
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
}
