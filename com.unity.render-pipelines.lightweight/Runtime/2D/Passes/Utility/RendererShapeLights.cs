using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class RendererShapeLights
    {
        /*static private RenderTextureFormat m_RenderTextureFormatToUse;
        static _2DShapeLightTypeDescription[] m_LightTypes;
        static RenderTargetHandle[] m_RenderTargets;
        static bool[] m_RenderTargetsDirty;
        static Camera m_Camera;
        static RenderTexture m_FullScreenShadowTexture = null;
        const string k_UseShapeLightTypeKeyword = "USE_SHAPE_LIGHT_TYPE_";

        private delegate void PerLightTypeAction(int lightTypeIndex);
        static private void DoPerLightTypeActions(PerLightTypeAction action)
        {
            for (int i = 0; i < m_LightTypes.Length; ++i)
            {
                if (!m_LightTypes[i].enabled)
                    continue;

                action(i);
            }
        }

        static public void Setup(_2DShapeLightTypeDescription[] lightTypes, Camera camera)
        {
            m_RenderTextureFormatToUse = RenderTextureFormat.ARGB32;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                m_RenderTextureFormatToUse = RenderTextureFormat.RGB111110Float;
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                m_RenderTextureFormatToUse = RenderTextureFormat.ARGBHalf;

            m_Camera = camera;
            m_LightTypes = lightTypes;

            m_RenderTargets = new RenderTargetHandle[m_LightTypes.Length];
            DoPerLightTypeActions(i => { m_RenderTargets[i].Init("_ShapeLightTexture" + i); });

            m_RenderTargetsDirty = new bool[m_LightTypes.Length];
        }

        static public void CreateRenderTextures(ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Create Shape Light Textures");
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor();
            descriptor.colorFormat = m_RenderTextureFormatToUse;
            descriptor.sRGB = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            DoPerLightTypeActions(i =>
            {
                float renderTextureScale = Mathf.Clamp(m_LightTypes[i].renderTextureScale, 0.01f, 1.0f);
                descriptor.width = (int)(m_Camera.pixelWidth * renderTextureScale);
                descriptor.height = (int)(m_Camera.pixelHeight * renderTextureScale);
                cmd.GetTemporaryRT(m_RenderTargets[i].id, descriptor, FilterMode.Bilinear);
                m_RenderTargetsDirty[i] = true;
            });

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        static public void ReleaseRenderTextures(ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Release Shape Light Textures");
            DoPerLightTypeActions(i => { cmd.ReleaseTemporaryRT(m_RenderTargets[i].id); });
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        static private bool RenderLightSet(Camera camera, Light2D.ShapeLightType type, CommandBuffer cmdBuffer, int layerToRender, List<Light2D> lights)
        {
            bool renderedAnyLight = false;

            foreach (var light in lights)
            {
                if (light != null && light.shapeLightType == type && light.IsLitLayer(layerToRender) && light.IsLightVisible(camera))
                {
                    Material shapeLightMaterial = light.GetMaterial();
                    if (shapeLightMaterial != null)
                    {
                        Mesh lightMesh = light.GetMesh();
                        if (lightMesh != null)
                        {
                            if (!renderedAnyLight)
                                renderedAnyLight = true;

                            cmdBuffer.DrawMesh(lightMesh, light.transform.localToWorldMatrix, shapeLightMaterial);
                        }
                    }
                }
            }

            return renderedAnyLight;
        }

        static private void RenderLightVolumeSet(Camera camera, Light2D.ShapeLightType type, CommandBuffer cmdBuffer, int layerToRender, RenderTargetIdentifier renderTexture, List<Light2D> lights)
        {
            if (lights.Count > 0)
            {
                for (int i = 0; i < lights.Count; i++)
                {
                    Light2D light = lights[i];

                    if (light != null && light.LightVolumeOpacity > 0.0f && light.shapeLightType == type && light.IsLitLayer(layerToRender) && light.IsLightVisible(camera))
                    {
                        Material shapeLightVolumeMaterial = light.GetVolumeMaterial();
                        if (shapeLightVolumeMaterial != null)
                        {
                            Mesh lightMesh = light.GetMesh();
                            if (lightMesh != null)
                            {
                                cmdBuffer.DrawMesh(lightMesh, light.transform.localToWorldMatrix, shapeLightVolumeMaterial);
                            }
                        }
                    }
                }
            }
        }

        static public void SetShaderGlobals(CommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalTexture("_ShadowTex", m_FullScreenShadowTexture);

            DoPerLightTypeActions(i =>
            {
                cmdBuffer.SetGlobalVector("_ShapeLightBlendFactors" + i, m_LightTypes[i].blendFactors);
                cmdBuffer.SetGlobalVector("_ShapeLightMaskFilter" + i, m_LightTypes[i].maskTextureChannelFilter);
            });
        }

        static public void RenderLights(Camera camera, CommandBuffer cmdBuffer, int layerToRender)
        {
            for (int i = 0; i < m_LightTypes.Length; ++i)
            {
                string keyword = k_UseShapeLightTypeKeyword + i;

                if (m_LightTypes[i].enabled)
                    cmdBuffer.EnableShaderKeyword(keyword);
                else
                    cmdBuffer.DisableShaderKeyword(keyword);
            }

            DoPerLightTypeActions(i =>
            {
                string sampleName = "2D Shape Lights - " + m_LightTypes[i].name;
                cmdBuffer.BeginSample(sampleName);

                cmdBuffer.SetRenderTarget(m_RenderTargets[i].Identifier());

                if (m_RenderTargetsDirty[i])
                    cmdBuffer.ClearRenderTarget(false, true, m_LightTypes[i].globalColor);

                var lightType = (Light2D.ShapeLightType)i;
                bool rtDirty = RenderLightSet(
                    camera,
                    lightType,
                    cmdBuffer,
                    layerToRender,
                    Light2D.GetShapeLights(lightType)
                );

                m_RenderTargetsDirty[i] = rtDirty;

                cmdBuffer.EndSample(sampleName);
            });
        }

        static public void RenderLightVolumes(Camera camera, CommandBuffer cmdBuffer, int layerToRender)
        {
            DoPerLightTypeActions(i =>
            {
                string sampleName = "2D Shape Light Volumes - " + m_LightTypes[i].name;
                cmdBuffer.BeginSample(sampleName);

                var lightType = (Light2D.ShapeLightType)i;
                RenderLightVolumeSet(
                    camera,
                    lightType,
                    cmdBuffer,
                    layerToRender,
                    m_RenderTargets[i].Identifier(),
                    Light2D.GetShapeLights(lightType)
                );

                cmdBuffer.EndSample(sampleName);
            });
        }*/
    }
}
