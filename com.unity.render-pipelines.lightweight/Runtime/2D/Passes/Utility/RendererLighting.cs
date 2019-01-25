using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class RendererLighting
    {
        static private RenderTextureFormat m_RenderTextureFormatToUse;
        static _2DLightOperationDescription[] m_LightTypes;
        static RenderTargetHandle[] m_RenderTargets;
        static bool[] m_RenderTargetsDirty;
        static Camera m_Camera;
        const string k_UseLightOperationKeyword = "USE_SHAPE_LIGHT_TYPE_";

        const int k_NormalsRenderingPassIndex = 1;
        static ShaderTagId m_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static CommandBuffer m_TemporaryCmdBuffer = new CommandBuffer();

        static Color k_NormalClearColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
        static RenderTargetHandle m_NormalsTarget;
        static Texture m_LightLookupTexture = GetLightLookupTexture();

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

        static public void Setup(_2DLightOperationDescription[] lightTypes, Camera camera)
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

            m_NormalsTarget = new RenderTargetHandle();
            m_NormalsTarget.Init("_NormalMap");

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

            descriptor.width = (int)(m_Camera.pixelWidth);
            descriptor.height = (int)(m_Camera.pixelHeight);
            cmd.GetTemporaryRT(m_NormalsTarget.id, descriptor, FilterMode.Bilinear);

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
            cmd.ReleaseTemporaryRT(m_NormalsTarget.id);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        static private bool RenderShapeLightSet(Camera camera, Light2D.LightOperation type, CommandBuffer cmdBuffer, int layerToRender, List<Light2D> lights, Light2D.LightProjectionTypes lightProjectionType)
        {
            bool renderedAnyLight = false;

            foreach (var light in lights)
            {
                if (light != null && light.GetLightProjectionType() == lightProjectionType && light.lightOperation == type && light.IsLitLayer(layerToRender) && light.IsLightVisible(camera))
                {
                    RendererLighting.SetPointLightShaderGlobals(cmdBuffer, light);

                    Material shapeLightMaterial = light.GetMaterial();
                    if (shapeLightMaterial != null)
                    {
                        Mesh lightMesh = light.GetMesh();
                        if (lightMesh != null)
                        {
                            if (!renderedAnyLight)
                                renderedAnyLight = true;

                            if (lightProjectionType == Light2D.LightProjectionTypes.Shape)
                            {
                                cmdBuffer.DrawMesh(lightMesh, light.transform.localToWorldMatrix, shapeLightMaterial);
                            }
                            else
                            {
                                //Vector3 scale = new Vector3(2 * light.m_PointLightOuterRadius, 2 * light.m_PointLightOuterRadius, 1);
                                Vector3 scale = new Vector3(light.m_PointLightOuterRadius, light.m_PointLightOuterRadius, light.m_PointLightOuterRadius);
                                Matrix4x4 matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                                cmdBuffer.DrawMesh(lightMesh, matrix, shapeLightMaterial);
                            }
                        }
                    }
                }
            }

            return renderedAnyLight;
        }

        static private void RenderLightVolumeSet(Camera camera, Light2D.LightOperation type, CommandBuffer cmdBuffer, int layerToRender, RenderTargetIdentifier renderTexture, List<Light2D> lights, Light2D.LightProjectionTypes lightProjectionType)
        {
            if (lights.Count > 0)
            {
                for (int i = 0; i < lights.Count; i++)
                {
                    Light2D light = lights[i];

                    if (light != null && light.GetLightProjectionType() == lightProjectionType && light.LightVolumeOpacity > 0.0f && light.lightOperation == type && light.IsLitLayer(layerToRender) && light.IsLightVisible(camera))
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

        static public void SetShapeLightShaderGlobals(CommandBuffer cmdBuffer)
        {
            DoPerLightTypeActions(i =>
            {
                cmdBuffer.SetGlobalVector("_ShapeLightBlendFactors" + i, m_LightTypes[i].blendFactors);
                cmdBuffer.SetGlobalVector("_ShapeLightMaskFilter" + i, m_LightTypes[i].maskTextureChannelFilter);
            });
        }

        static Texture GetLightLookupTexture()
        {
            if (m_LightLookupTexture == null)
                m_LightLookupTexture = Light2DLookupTexture.CreateLightLookupTexture();

            return m_LightLookupTexture;
        }

        static public float GetNormalizedInnerRadius(Light2D light)
        {
            return light.m_PointLightInnerRadius / light.m_PointLightOuterRadius;
        }

        static public float GetNormalizedAngle(float angle)
        {
            return (angle / 360.0f);
        }

        static public void GetScaledLightInvMatrix(Light2D light, out Matrix4x4 retMatrix, bool includeRotation)
        {
            float outerRadius = light.m_PointLightOuterRadius;
            //Vector3 lightScale = light.transform.lossyScale;
            Vector3 lightScale = Vector3.one;
            Vector3 outerRadiusScale = new Vector3(lightScale.x * outerRadius, lightScale.y * outerRadius, lightScale.z * outerRadius);

            Quaternion rotation;
            if (includeRotation)
                rotation = light.transform.rotation;
            else
                rotation = Quaternion.identity;

            Matrix4x4 scaledLightMat = Matrix4x4.TRS(light.transform.position, rotation, outerRadiusScale);
            retMatrix = Matrix4x4.Inverse(scaledLightMat);
        }

        static public void SetPointLightShaderGlobals(CommandBuffer cmdBuffer, Light2D light)
        {
            cmdBuffer.SetGlobalColor("_LightColor", light.m_LightColor);
            cmdBuffer.SetGlobalColor("_LightVolumeColor", new Color(1, 1, 1, light.LightVolumeOpacity));

            //=====================================================================================
            //                          New stuff
            //=====================================================================================
            // This is used for the lookup texture
            Matrix4x4 lightInverseMatrix;
            Matrix4x4 lightNoRotInverseMatrix;
            GetScaledLightInvMatrix(light, out lightInverseMatrix, true);
            GetScaledLightInvMatrix(light, out lightNoRotInverseMatrix, false);

            float innerRadius = GetNormalizedInnerRadius(light);
            float innerAngle = GetNormalizedAngle(light.m_PointLightInnerAngle);
            float outerAngle = GetNormalizedAngle(light.m_PointLightOuterAngle);
            float innerRadiusMult = 1 / (1 - innerRadius);

            cmdBuffer.SetGlobalVector("_LightPosition", light.transform.position);
            cmdBuffer.SetGlobalMatrix("_LightInvMatrix", lightInverseMatrix);
            cmdBuffer.SetGlobalMatrix("_LightNoRotInvMatrix", lightNoRotInverseMatrix);
            cmdBuffer.SetGlobalFloat("_InnerRadiusMult", innerRadiusMult);

            cmdBuffer.SetGlobalFloat("_OuterAngle", outerAngle);
            cmdBuffer.SetGlobalFloat("_InnerAngleMult", 1 / (outerAngle - innerAngle));
            cmdBuffer.SetGlobalTexture("_LightLookup", GetLightLookupTexture());

            cmdBuffer.SetGlobalFloat("_LightZDistance", light.m_PointLightZDistance);

            if (light.m_LightCookieSprite != null && light.m_LightCookieSprite.texture != null)
            {
                cmdBuffer.EnableShaderKeyword("USE_POINT_LIGHT_COOKIES");
                cmdBuffer.SetGlobalTexture("_PointLightCookieTex", light.m_LightCookieSprite.texture);
            }
            else
            {
                cmdBuffer.DisableShaderKeyword("USE_POINT_LIGHT_COOKIES");
            }
        }


        static public void RenderNormals(ScriptableRenderContext renderContext, CullingResults cullResults, DrawingSettings drawSettings, FilteringSettings filterSettings)
        {
            m_TemporaryCmdBuffer.name = "Clear Normals";
            m_TemporaryCmdBuffer.Clear();
            m_TemporaryCmdBuffer.SetRenderTarget(m_NormalsTarget.Identifier());
            m_TemporaryCmdBuffer.ClearRenderTarget(true, true, k_NormalClearColor);
            renderContext.ExecuteCommandBuffer(m_TemporaryCmdBuffer);
            drawSettings.SetShaderPassName(0, m_NormalsRenderingPassName);
            renderContext.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
        }


        static public void RenderLights(Camera camera, CommandBuffer cmdBuffer, int layerToRender)
        {
            for (int i = 0; i < m_LightTypes.Length; ++i)
            {
                string keyword = k_UseLightOperationKeyword + i;

                if (m_LightTypes[i].enabled)
                    cmdBuffer.EnableShaderKeyword(keyword);
                else
                    cmdBuffer.DisableShaderKeyword(keyword);
            }

            DoPerLightTypeActions(i =>
            {
                string sampleName = "2D Point Lights - " + m_LightTypes[i].name;
                cmdBuffer.BeginSample(sampleName);

                cmdBuffer.SetRenderTarget(m_RenderTargets[i].Identifier());

                if (m_RenderTargetsDirty[i])
                    cmdBuffer.ClearRenderTarget(false, true, m_LightTypes[i].globalColor);

                var lightType = (Light2D.LightOperation)i;
                bool rtDirty = RenderShapeLightSet(
                    camera,
                    lightType,
                    cmdBuffer,
                    layerToRender,
                    Light2D.GetShapeLights(lightType),
                    Light2D.LightProjectionTypes.Shape
                );

                rtDirty |= RenderShapeLightSet(
                    camera,
                    lightType,
                    cmdBuffer,
                    layerToRender,
                    Light2D.GetShapeLights(lightType),
                    Light2D.LightProjectionTypes.Point
                );


                m_RenderTargetsDirty[i] = rtDirty;

                cmdBuffer.EndSample(sampleName);
            });
        }

        static public void RenderLightVolumes(Camera camera, CommandBuffer cmdBuffer, int layerToRender, Light2D.LightProjectionTypes lightProjectionType)
        {
            DoPerLightTypeActions(i =>
            {
                string sampleName = "2D Shape Light Volumes - " + m_LightTypes[i].name;
                cmdBuffer.BeginSample(sampleName);

                var lightType = (Light2D.LightOperation)i;
                RenderLightVolumeSet(
                    camera,
                    lightType,
                    cmdBuffer,
                    layerToRender,
                    m_RenderTargets[i].Identifier(),
                    Light2D.GetShapeLights(lightType),
                    lightProjectionType
                );

                cmdBuffer.EndSample(sampleName);
            });
        }
    }
}
