using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    public class LightLoop
    {
        public virtual string GetKeyword()
        {
            return "";
        }

        public virtual void Rebuild()
        {
            m_lightList = new LightList();

            s_DirectionalLights = new ComputeBuffer(HDRenderLoop.k_MaxDirectionalLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightData)));
            s_DirectionalShadowList = new ComputeBuffer(HDRenderLoop.k_MaxCascadeCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalShadowData)));
            s_PunctualLightList = new ComputeBuffer(HDRenderLoop.k_MaxPunctualLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
            s_AreaLightList = new ComputeBuffer(HDRenderLoop.k_MaxAreaLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
            s_EnvLightList = new ComputeBuffer(HDRenderLoop.k_MaxEnvLightsOnSCreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
            s_PunctualShadowList = new ComputeBuffer(HDRenderLoop.k_MaxShadowOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PunctualShadowData)));

            m_CookieTexArray = new TextureCache2D();
            m_CookieTexArray.AllocTextureArray(8, m_TextureSettings.spotCookieSize, m_TextureSettings.spotCookieSize, TextureFormat.RGBA32, true);
            m_CubeCookieTexArray = new TextureCacheCubemap();
            m_CubeCookieTexArray.AllocTextureArray(4, m_TextureSettings.pointCookieSize, TextureFormat.RGBA32, true);
            m_CubeReflTexArray = new TextureCacheCubemap();
            m_CubeReflTexArray.AllocTextureArray(32, m_TextureSettings.reflectionCubemapSize, TextureFormat.BC6H, true);
        }

        public virtual void Cleanup()
        {
            Utilities.SafeRelease(s_DirectionalLights);
            Utilities.SafeRelease(s_DirectionalShadowList);
            Utilities.SafeRelease(s_PunctualLightList);
            Utilities.SafeRelease(s_AreaLightList);
            Utilities.SafeRelease(s_EnvLightList);
            Utilities.SafeRelease(s_PunctualShadowList);

            if (m_CubeReflTexArray != null)
            {
                m_CubeReflTexArray.Release();
                m_CubeReflTexArray = null;
            }
            if (m_CookieTexArray != null)
            {
                m_CookieTexArray.Release();
                m_CookieTexArray = null;
            }
            if (m_CubeCookieTexArray != null)
            {
                m_CubeCookieTexArray.Release();
                m_CubeCookieTexArray = null;
            }
        }

        virtual void NewFrame()
        {
            m_CookieTexArray.NewFrame();
            m_CubeCookieTexArray.NewFrame();
            m_CubeReflTexArray.NewFrame();
        }



        public ShadowData GetShadowData(VisibleLight light, AdditionalLightData additionalData, int lightIndex, ref ShadowOutput shadowOutput)
        {
            bool hasDirectionalShadows = light.light.shadows != LightShadows.None && shadowOutput.GetShadowSliceCountLightIndex(lightIndex) != 0;

            if (hasDirectionalShadows)
            {
                for (int sliceIndex = 0; sliceIndex < shadowOutput.GetShadowSliceCountLightIndex(lightIndex); ++sliceIndex)
                {
                    ShadowData shadowData = new ShadowData();

                    int shadowSliceIndex = shadowOutput.GetShadowSliceIndex(lightIndex, sliceIndex);
                    shadowData.worldToShadow = shadowOutput.shadowSlices[shadowSliceIndex].shadowTransform.transpose; // Transpose for hlsl reading ?
                    shadowData.lightType = lightData.lightType;

                    shadowData.bias = light.light.shadowBias;

                    m_lightList.directionalShadows.Add(shadowData);
                }
            }
        }

        public DirectionalLightData GetDirectionalLightData(VisibleLight light, AdditionalLightData additionalData)
        {
            Debug.Assert(light.lightType == LightType.Directional);

            var directionalLightData = new DirectionalLightData();
            // Light direction for directional and is opposite to the forward direction
            directionalLightData.direction = -light.light.transform.forward;
            // up and right are use for cookie
            directionalLightData.up = light.light.transform.up;
            directionalLightData.right = light.light.transform.right;
            directionalLightData.positionWS = light.light.transform.position;
            directionalLightData.color = GetLightColor(light);
            directionalLightData.diffuseScale = additionalData.affectDiffuse ? 1.0f : 0.0f;
            directionalLightData.specularScale = additionalData.affectSpecular ? 1.0f : 0.0f;
            directionalLightData.invScaleX = 1.0f / light.light.transform.localScale.x;
            directionalLightData.invScaleY = 1.0f / light.light.transform.localScale.y;
            directionalLightData.cosAngle = 0.0f;
            directionalLightData.sinAngle = 0.0f;
            directionalLightData.shadowIndex = -1;
            directionalLightData.cookieIndex = -1;

            if (light.light.cookie != null)
            {
                directionalLightData.tileCookie = (light.light.cookie.wrapMode == TextureWrapMode.Repeat);
                directionalLightData.cookieIndex = m_CookieTexArray.FetchSlice(light.light.cookie);
            }

            directionalLightData.shadowIndex = 0;
        }

        public virtual void PrepareLightsForGPU(CullResults cullResults, Camera camera) { }

        public virtual void PushGlobalParams(Camera camera, RenderLoop loop)
        {
            Shader.SetGlobalTexture("_CookieTextures", m_CookieTexArray.GetTexCache());
            Shader.SetGlobalTexture("_CookieCubeTextures", m_CubeCookieTexArray.GetTexCache());
            Shader.SetGlobalTexture("_EnvTextures", m_CubeReflTexArray.GetTexCache());

            s_DirectionalLights.SetData(m_lightList.directionalLights.ToArray());
            s_DirectionalShadowList.SetData(m_lightList.directionalShadows.ToArray());
            s_PunctualLightList.SetData(m_lightList.punctualLights.ToArray());
            s_AreaLightList.SetData(m_lightList.areaLights.ToArray());
            s_EnvLightList.SetData(m_lightList.envLights.ToArray());
            s_PunctualShadowList.SetData(m_lightList.punctualShadows.ToArray());

            Shader.SetGlobalBuffer("_DirectionalLightList", s_DirectionalLights);
            Shader.SetGlobalInt("_DirectionalLightCount", m_lightList.directionalLights.Count);
            Shader.SetGlobalBuffer("_DirectionalShadowList", s_DirectionalShadowList);
            Shader.SetGlobalBuffer("_PunctualLightList", s_PunctualLightList);
            Shader.SetGlobalInt("_PunctualLightCount", m_lightList.punctualLights.Count);
            Shader.SetGlobalBuffer("_AreaLightList", s_AreaLightList);
            Shader.SetGlobalInt("_AreaLightCount", m_lightList.areaLights.Count);
            Shader.SetGlobalBuffer("_PunctualShadowList", s_PunctualShadowList);
            Shader.SetGlobalBuffer("_EnvLightList", s_EnvLightList);
            Shader.SetGlobalInt("_EnvLightCount", m_lightList.envLights.Count);

            Shader.SetGlobalVectorArray("_DirShadowSplitSpheres", m_lightList.directionalShadowSplitSphereSqr);     
        }

        public virtual void RenderDeferredLighting(Camera camera, RenderLoop renderLoop, RenderTargetIdentifier cameraColorBufferRT) {}
    }
}
