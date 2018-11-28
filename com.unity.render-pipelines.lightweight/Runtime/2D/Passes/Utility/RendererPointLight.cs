using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;
using System.Linq;

// Steps to convert
// 1) swap out single light rendering code to render to a render texture, and make sure our results stay the same
// 2) support multiple lights

// We should do this with several passes
// Normal map pass - render the standard geometry using a normal map. This will render back to front (for now) so turn off ztest/zwrite
// Light pass - render this using the normal map pass as input.


namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{

    public class RendererPointLights
    {
        //static bool m_SelfShadowing = true;

        static private RenderTextureFormat m_RenderTextureFormatToUse;
        //static Material m_HardShadowMaterial;
        //static Material m_SoftShadowMaterial;
        //static Material m_DefaultLightMaterial;
        static Color m_DefaultAmbientColor;
        static Color m_DefaultRimColor;
        static Color m_DefaultSpecularColor;
        static Texture m_LightLookupTexture = GetLightLookupTexture();

        const int k_NormalsRenderingPassIndex = 1;
        static ShaderTagId m_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        static CommandBuffer m_TemporaryCmdBuffer = new CommandBuffer();

        static Material m_PointLightingMat = GetPointLightMat();
        static Mesh m_Quad;

        static RenderTexture m_NormalRT;
        static RenderTexture m_ColorRT;

        static Color k_ClearColor = Color.black;


        static Texture GetLightLookupTexture()
        {
            if(m_LightLookupTexture == null)
                m_LightLookupTexture = Light2DLookupTexture.CreateLightLookupTexture();

            return m_LightLookupTexture;
        }

        static Material GetPointLightMat()
        {
            if(m_PointLightingMat == null)
            {
                Shader pointLightShader = Shader.Find("Hidden/Light2DPointLight");
                m_PointLightingMat = new Material(pointLightShader);
            }

            return m_PointLightingMat;
        }

        static Mesh GetQuadMesh()
        {
            if (m_Quad == null)
            {
                CreateQuad(out m_Quad);
            }

            return m_Quad;
        }

        static public void Initialize()
        {
            m_RenderTextureFormatToUse = RenderTextureFormat.ARGB32;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                m_RenderTextureFormatToUse = RenderTextureFormat.ARGBHalf;
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                m_RenderTextureFormatToUse = RenderTextureFormat.ARGBFloat;

            //m_HardShadowMaterial = Resources.Load<Material>("Materials/HardShadow");
            //m_SoftShadowMaterial = Resources.Load<Material>("Materials/SoftShadow");
            //m_DefaultLightMaterial = Resources.Load<Material>("Materials/Light2D");

            //m_LightLookupTexture = Resources.Load<Texture>("Textures/LightLookupTexture");

            CreateQuad(out m_Quad);
        }

        static void CreateQuad(out Mesh outQuad)
        {
            Vector3[] vertices = new Vector3[4];
            int[] triangles = new int[6];
            vertices[0] = new Vector3(-0.5f, -0.5f, 0);
            vertices[1] = new Vector3(0.5f, -0.5f, 0);
            vertices[2] = new Vector3(0.5f, 0.5f, 0);
            vertices[3] = new Vector3(-0.5f, 0.5f, 0);

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 3;
            triangles[3] = 1;
            triangles[4] = 2;
            triangles[5] = 3;

            outQuad = new Mesh();
            outQuad.vertices = vertices;
            outQuad.triangles = triangles;
        }

        static float Square(float f) { return f * f; }

        static void SortShadowCasters(Light2D light, List<GameObject> shadowCasters)
        {
            Vector3 lightPos = light.transform.position;
            shadowCasters.Sort(
                delegate (GameObject go1, GameObject go2)
                {
                    Vector3 go1Pos = go1.transform.position;
                    Vector3 go2Pos = go2.transform.position;
                    float dist1 = Square(go1Pos.x - lightPos.x) + Square(go1Pos.y - lightPos.y);
                    float dist2 = Square(go2Pos.x - lightPos.x) + Square(go2Pos.y - lightPos.y);

                    return dist2.CompareTo(dist1);
                }
            );
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

        static public void CreateRenderTextures(Light2DRTInfo normalTexture, Light2DRTInfo colorTexture)
        {
            m_NormalRT = normalTexture.GetRenderTexture(m_RenderTextureFormatToUse);
            m_ColorRT = colorTexture.GetRenderTexture(m_RenderTextureFormatToUse);
        }

        static public void ReleaseRenderTextures()
        {
            if (m_NormalRT != null)
            {
                RenderTexture.ReleaseTemporary(m_NormalRT);
                m_NormalRT = null;
            }

            if (m_ColorRT != null)
            {
                RenderTexture.ReleaseTemporary(m_ColorRT);
                m_ColorRT = null;
            }
        }

        static public void DrawLightQuad(CommandBuffer cmdBuffer, Light2D light, RenderTexture sourceRT, Material material)
        {
            Vector3 scale = new Vector3(2*light.m_PointLightOuterRadius, 2*light.m_PointLightOuterRadius, 1);
            Matrix4x4 matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);

            if (material != null)
            {
                material.SetTexture("_MainTex", sourceRT);
                cmdBuffer.DrawMesh(GetQuadMesh(), matrix, material);
            }
        }

        static public void Clear(CommandBuffer cmdBuffer)
        {
            cmdBuffer.SetGlobalColor("_PointLightColor", Color.black);
            cmdBuffer.SetGlobalVector("_PointLightPosition", new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, 1));
            cmdBuffer.SetGlobalFloat("_PointLightOuterRadius", 0.1f);
            cmdBuffer.SetGlobalFloat("_PointLightInnerRadius", 0.0f);
        }

        static void RenderNormals(ScriptableRenderContext renderContext, CullingResults cullResults, DrawingSettings drawSettings, FilteringSettings filterSettings)
        {
            m_TemporaryCmdBuffer.name = "Render Normals";
            m_TemporaryCmdBuffer.Clear();
            m_TemporaryCmdBuffer.SetRenderTarget(m_NormalRT);
            m_TemporaryCmdBuffer.ClearRenderTarget(true, true, k_ClearColor); 
            renderContext.ExecuteCommandBuffer(m_TemporaryCmdBuffer);
            drawSettings.SetShaderPassName(0, m_NormalsRenderingPassName);
            renderContext.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
        }

        static public void RenderLights(CommandBuffer cmdBuffer, ScriptableRenderContext renderContext, CullingResults cullResults, DrawingSettings drawSettings, FilteringSettings filterSettings, int layerToRender, Camera camera)
        {
            //List<GameObject> shadowCasters = GameObject.FindGameObjectsWithTag("Shadow").ToList();
            cmdBuffer.DisableShaderKeyword("USE_POINT_LIGHTS");
            cmdBuffer.SetGlobalTexture("_PointLightingTex", m_ColorRT);

            List<Light2D> pointLights = Light2D.GetPointLights();
            if (pointLights != null && pointLights.Count > 0)
            {
                RenderNormals(renderContext, cullResults, drawSettings, filterSettings);

                cmdBuffer.BeginSample("2D Point Lights");
                //cmdBuffer.EnableShaderKeyword("USE_POINT_LIGHTS");

                cmdBuffer.SetRenderTarget(m_ColorRT);
                cmdBuffer.ClearRenderTarget(true, true, k_ClearColor);

                for (int i = 0; i < pointLights.Count; i++)
                {
                    Light2D light = pointLights[i];

                    if (light.IsLitLayer(layerToRender) && light.isActiveAndEnabled)
                    {
                        // Sort the shadow casters by distance to light, and render the ones furthest first
                        //SortShadowCasters(light, shadowCasters);

                        // Consolidate these later...

                        cmdBuffer.SetGlobalColor("_LightColor", light.m_LightColor);

                        //=====================================================================================
                        //                          Old stuff
                        //=====================================================================================
                        cmdBuffer.SetGlobalColor("_PointLightColor", light.m_LightColor);
                        cmdBuffer.SetGlobalVector("_PointLightPosition", light.transform.position);
                        cmdBuffer.SetGlobalFloat("_PointLightOuterRadius", light.m_PointLightOuterRadius);
                        cmdBuffer.SetGlobalFloat("_PointLightInnerRadius", light.m_PointLightInnerRadius >= (light.m_PointLightOuterRadius - 0.0001f) ? light.m_PointLightOuterRadius - 0.0001f : light.m_PointLightInnerRadius);
                        cmdBuffer.SetGlobalColor("_PointLightShadowColor", light.m_ShadowColor);

                        float pointLightInnerAngle = Mathf.Deg2Rad * 0.5f * light.m_PointLightInnerAngle;
                        float pointLightOuterAngle = Mathf.Deg2Rad * 0.5f * light.m_PointLightOuterAngle;
                        cmdBuffer.SetGlobalFloat("_PointLightInnerAngle", pointLightInnerAngle);
                        cmdBuffer.SetGlobalFloat("_PointLightOuterAngle", pointLightOuterAngle);
                        cmdBuffer.SetGlobalVector("_PointLightForward", light.transform.rotation * Vector3.up);

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

                        cmdBuffer.SetGlobalMatrix("_LightInvMatrix", lightInverseMatrix);
                        cmdBuffer.SetGlobalMatrix("_LightNoRotInvMatrix", lightNoRotInverseMatrix);
                        cmdBuffer.SetGlobalFloat("_InnerRadiusMult", innerRadiusMult);

                        cmdBuffer.SetGlobalFloat("_OuterAngle", outerAngle);
                        cmdBuffer.SetGlobalFloat("_InnerAngleMult", 1 / (outerAngle - innerAngle));
                        cmdBuffer.SetGlobalTexture("_LightLookup", GetLightLookupTexture());

                        if (light.m_LightCookieSprite != null && light.m_LightCookieSprite.texture != null)
                        {
                            cmdBuffer.EnableShaderKeyword("USE_POINT_LIGHT_COOKIES");
                            cmdBuffer.SetGlobalTexture("_PointLightCookieTex", light.m_LightCookieSprite.texture);
                        }
                        else
                        {
                            cmdBuffer.DisableShaderKeyword("USE_POINT_LIGHT_COOKIES");
                        }

                        // We should consider combining all point lights into a single pass instead of rendering them seperately
                        DrawLightQuad(cmdBuffer, light, m_NormalRT, GetPointLightMat());

                        //m_PointLightingMat.SetTexture("_MainTex", m_NormalRT);
                        //ScriptableRenderer.RenderFullscreenQuad(cmdBuffer, m_PointLightingMat);

                        //cmdBuffer.SetRenderTarget(m_FullScreenShadowTexture);
                        //cmdBuffer.ClearRenderTarget(true, true, Color.clear, 1.0f);
                        //if (light.m_CastsShadows && shadowCasters != null && shadowCasters.Count > 0)
                        //{

                        //// Render all the hard shadows
                        //foreach (GameObject shadowCaster in shadowCasters)
                        //{
                        //    MeshFilter shadowRenderer = shadowCaster.GetComponent<MeshFilter>();
                        //    if (shadowRenderer != null)
                        //    {
                        //        cmdBuffer.DrawMesh(shadowRenderer.sharedMesh, shadowRenderer.transform.localToWorldMatrix, m_HardShadowMaterial);
                        //    }
                        //}
                        //}
                    }
                }
                cmdBuffer.EndSample("2D Point Lights");
            }
            else
            {
                cmdBuffer.SetRenderTarget(m_ColorRT);
                cmdBuffer.ClearRenderTarget(true, true, k_ClearColor);
            }

            renderContext.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Clear();
        }

        static public void SetShaderGlobals(CommandBuffer cmdBuffer)
        {
            //cmdBuffer.SetGlobalColor("_AmbientColor", m_DefaultAmbientColor);
            //cmdBuffer.SetGlobalTexture("_ShadowTex", m_FullScreenShadowTexture);
        }

        //static public void Render(int layerToRender, ScriptableRenderContext renderContext, Camera camera)
        //{
        //    RenderPointLights(layerToRender, renderContext, camera);  // This needs to be re-evaluated. Because the light intensity is unfortunately not layer independent
        //    cmdBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

        //}
    }
}
