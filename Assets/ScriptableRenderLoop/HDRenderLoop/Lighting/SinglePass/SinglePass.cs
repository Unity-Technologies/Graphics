using UnityEngine;
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

            ComputeBuffer m_punctualLightList;
            ComputeBuffer m_envLightList;
            ComputeBuffer m_areaLightList;
            ComputeBuffer m_punctualShadowList;

            void ClearComputeBuffers()
            {
                if (m_punctualLightList != null)
                    m_punctualLightList.Release();

                if (m_areaLightList != null)
                    m_areaLightList.Release();

                if (m_punctualShadowList != null)
                    m_punctualShadowList.Release();

                if (m_envLightList != null)
                    m_envLightList.Release();
            }

            public void Rebuild()
            {
                ClearComputeBuffers();

                m_punctualLightList = new ComputeBuffer(HDRenderLoop.k_MaxPunctualLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualLightData)));
                m_areaLightList = new ComputeBuffer(HDRenderLoop.k_MaxAreaLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(AreaLightData)));
                m_envLightList = new ComputeBuffer(HDRenderLoop.k_MaxEnvLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
                m_punctualShadowList = new ComputeBuffer(HDRenderLoop.k_MaxShadowOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualShadowData)));
            }

            public void OnDisable()
            {
                m_punctualLightList.Release();
                m_areaLightList.Release();
                m_envLightList.Release();
                m_punctualShadowList.Release();
            }

            public void PushGlobalParams(Camera camera, RenderLoop loop, HDRenderLoop.LightList lightList)
            {
                m_punctualLightList.SetData(lightList.punctualLights.ToArray());
                m_areaLightList.SetData(lightList.areaLights.ToArray());
                m_envLightList.SetData(lightList.envLights.ToArray());
                m_punctualShadowList.SetData(lightList.punctualShadows.ToArray());

                Shader.SetGlobalBuffer("_PunctualLightList", m_punctualLightList);
                Shader.SetGlobalInt("_PunctualLightCount", lightList.punctualLights.Count);
                Shader.SetGlobalBuffer("_AreaLightList", m_areaLightList);
                Shader.SetGlobalInt("_AreaLightCount", lightList.areaLights.Count);  
                Shader.SetGlobalBuffer("_PunctualShadowList", m_punctualShadowList);
                Shader.SetGlobalBuffer("_EnvLightList", m_envLightList);
                Shader.SetGlobalInt("_EnvLightCount", lightList.envLights.Count);         
            }
        }
    }
}
