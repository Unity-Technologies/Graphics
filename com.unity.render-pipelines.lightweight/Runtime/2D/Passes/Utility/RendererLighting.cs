using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal static class RendererLighting
    {
        static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
        static readonly string[] k_UseLightOperationKeywords =
        {
            "USE_SHAPE_LIGHT_TYPE_0",
            "USE_SHAPE_LIGHT_TYPE_1",
            "USE_SHAPE_LIGHT_TYPE_2",
            "USE_SHAPE_LIGHT_TYPE_3"
        };

        static _2DLightOperationDescription[] s_LightOperations;
        static RenderTargetHandle[] s_RenderTargets;
        static bool[] s_RenderTargetsDirty;
        static RenderTargetHandle s_NormalsTarget;
        static Texture s_LightLookupTexture;

        private delegate void PerLightTypeAction(int lightTypeIndex);
        static private void DoPerLightTypeActions(PerLightTypeAction action)
        {
            for (int i = 0; i < s_LightOperations.Length; ++i)
            {
                if (!s_LightOperations[i].enabled)
                    continue;

                action(i);
            }
        }

        static public void Setup(_2DLightOperationDescription[] lightTypes)
        {
            s_LightOperations = lightTypes;

            s_RenderTargets = new RenderTargetHandle[s_LightOperations.Length];
            DoPerLightTypeActions(i => { s_RenderTargets[i].Init("_ShapeLightTexture" + i); });

            s_NormalsTarget = new RenderTargetHandle();
            s_NormalsTarget.Init("_NormalMap");

            s_RenderTargetsDirty = new bool[s_LightOperations.Length];
        }

        static public void CreateRenderTextures(CommandBuffer cmd, Camera camera)
        {
            var renderTextureFormatToUse = RenderTextureFormat.ARGB32;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                renderTextureFormatToUse = RenderTextureFormat.RGB111110Float;
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                renderTextureFormatToUse = RenderTextureFormat.ARGBHalf;

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor();
            descriptor.colorFormat = renderTextureFormatToUse;
            descriptor.sRGB = false;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            descriptor.dimension = TextureDimension.Tex2D;

            descriptor.width = (int)(camera.pixelWidth);
            descriptor.height = (int)(camera.pixelHeight);
            cmd.GetTemporaryRT(s_NormalsTarget.id, descriptor, FilterMode.Bilinear);

            DoPerLightTypeActions(i =>
            {
                float renderTextureScale = Mathf.Clamp(s_LightOperations[i].renderTextureScale, 0.01f, 1.0f);
                descriptor.width = (int)(camera.pixelWidth * renderTextureScale);
                descriptor.height = (int)(camera.pixelHeight * renderTextureScale);
                cmd.GetTemporaryRT(s_RenderTargets[i].id, descriptor, FilterMode.Bilinear);
                s_RenderTargetsDirty[i] = true;
            });
        }

        static public void ReleaseRenderTextures(CommandBuffer cmd)
        {
            DoPerLightTypeActions(i => { cmd.ReleaseTemporaryRT(s_RenderTargets[i].id); });
            cmd.ReleaseTemporaryRT(s_NormalsTarget.id);
        }

        static private bool RenderShapeLightSet(Camera camera, Light2D.LightOperation type, CommandBuffer cmdBuffer, int layerToRender, List<Light2D> lights, Light2D.LightProjectionTypes lightProjectionType)
        {
            bool renderedAnyLight = false;

            foreach (var light in lights)
            {
                if (light != null && light.GetLightProjectionType() == lightProjectionType && light.lightOperation == type && light.IsLitLayer(layerToRender) && light.IsLightVisible(camera))
                {
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
                                RendererLighting.SetPointLightShaderGlobals(cmdBuffer, light);
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
                                if (lightProjectionType == Light2D.LightProjectionTypes.Shape)
                                {
                                    cmdBuffer.DrawMesh(lightMesh, light.transform.localToWorldMatrix, shapeLightVolumeMaterial);
                                }
                                else
                                {
                                    RendererLighting.SetPointLightShaderGlobals(cmdBuffer, light);
                                    //Vector3 scale = new Vector3(2 * light.m_PointLightOuterRadius, 2 * light.m_PointLightOuterRadius, 1);
                                    Vector3 scale = new Vector3(light.m_PointLightOuterRadius, light.m_PointLightOuterRadius, light.m_PointLightOuterRadius);
                                    Matrix4x4 matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                                    cmdBuffer.DrawMesh(lightMesh, matrix, shapeLightVolumeMaterial);
                                }
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
                cmdBuffer.SetGlobalVector("_ShapeLightBlendFactors" + i, s_LightOperations[i].blendFactors);
                cmdBuffer.SetGlobalVector("_ShapeLightMaskFilter" + i, s_LightOperations[i].maskTextureChannelFilter);
            });
        }

        static Texture GetLightLookupTexture()
        {
            if (s_LightLookupTexture == null)
                s_LightLookupTexture = Light2DLookupTexture.CreateLightLookupTexture();

            return s_LightLookupTexture;
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

            if (light.m_LightQuality == Light2D.LightQuality.Fast)
            {
                cmdBuffer.EnableShaderKeyword("LIGHT_QUALITY_FAST");
                cmdBuffer.DisableShaderKeyword("LIGHT_QUALITY_ACCURATE");
            }
            else
            {
                cmdBuffer.DisableShaderKeyword("LIGHT_QUALITY_FAST");
                cmdBuffer.EnableShaderKeyword("LIGHT_QUALITY_ACCURATE");
            }

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
            var cmd = CommandBufferPool.Get("Clear Normals");
            cmd.SetRenderTarget(s_NormalsTarget.Identifier());
            cmd.ClearRenderTarget(true, true, k_NormalClearColor);
            renderContext.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            drawSettings.SetShaderPassName(0, k_NormalsRenderingPassName);
            renderContext.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
        }

        static public void RenderLights(Camera camera, CommandBuffer cmdBuffer, int layerToRender)
        {
            for (int i = 0; i < s_LightOperations.Length; ++i)
            {
                if (i >= k_UseLightOperationKeywords.Length)
                    break;

                string keyword = k_UseLightOperationKeywords[i];

                if (s_LightOperations[i].enabled)
                    cmdBuffer.EnableShaderKeyword(keyword);
                else
                    cmdBuffer.DisableShaderKeyword(keyword);
            }

            DoPerLightTypeActions(i =>
            {
                string sampleName = "2D Point Lights - " + s_LightOperations[i].name;
                cmdBuffer.BeginSample(sampleName);

                cmdBuffer.SetRenderTarget(s_RenderTargets[i].Identifier());

                if (s_RenderTargetsDirty[i])
                    cmdBuffer.ClearRenderTarget(false, true, s_LightOperations[i].globalColor);

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


                s_RenderTargetsDirty[i] = rtDirty;

                cmdBuffer.EndSample(sampleName);
            });
        }

        static public void RenderLightVolumes(Camera camera, CommandBuffer cmdBuffer, int layerToRender, Light2D.LightProjectionTypes lightProjectionType)
        {
            DoPerLightTypeActions(i =>
            {
                string sampleName = "2D Shape Light Volumes - " + s_LightOperations[i].name;
                cmdBuffer.BeginSample(sampleName);

                var lightType = (Light2D.LightOperation)i;
                RenderLightVolumeSet(
                    camera,
                    lightType,
                    cmdBuffer,
                    layerToRender,
                    s_RenderTargets[i].Identifier(),
                    Light2D.GetShapeLights(lightType),
                    lightProjectionType
                );

                cmdBuffer.EndSample(sampleName);
            });
        }
    }
}
