using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Deferred2DShadingPass : ScriptableRenderPass
{
    static RenderTargetHandle s_GBufferColorTarget;
    static SortingLayer[] s_SortingLayers;
    static readonly ShaderTagId k_GBufferPassName = new ShaderTagId("Universal2DGBuffer");
    static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId>() { k_GBufferPassName };

    Renderer2DData m_RendererData;
    Material m_GlobalLightMaterial;
    Material m_ShapeLightMaterial;
    Material m_PointLightMaterial;

    public Deferred2DShadingPass(Renderer2DData rendererData)
    {
        if (s_SortingLayers == null)
            s_SortingLayers = SortingLayer.layers;

        m_RendererData = rendererData;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("2D Deferred Shading");

        // Create g-buffer RTs.
        if (s_GBufferColorTarget.id == 0)
            s_GBufferColorTarget.Init("_GBufferColor");

        ref var targetDescriptor = ref renderingData.cameraData.cameraTargetDescriptor;
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(targetDescriptor.width, targetDescriptor.height);
        descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
        descriptor.useMipMap = false;
        descriptor.autoGenerateMips = false;
        descriptor.depthBufferBits = 0;
        descriptor.msaaSamples = 1;
        descriptor.dimension = TextureDimension.Tex2D;

        cmd.GetTemporaryRT(s_GBufferColorTarget.id, descriptor);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            s_SortingLayers = SortingLayer.layers;
#endif

        FilteringSettings filterSettings = new FilteringSettings();
        filterSettings.renderQueueRange = RenderQueueRange.all;
        filterSettings.layerMask = -1;
        filterSettings.renderingLayerMask = 0xFFFFFFFF;
        filterSettings.sortingLayerRange = SortingLayerRange.all;

        for (int i = 0; i < s_SortingLayers.Length; ++i)
        {
            string sortingLayerName = "Sorting Layer - " + s_SortingLayers[i].name;
            cmd.BeginSample(sortingLayerName);

            // Some renderers override their sorting layer value with short.MinValue or short.MaxValue.
            // When drawing the first sorting layer, we should include the range from short.MinValue to layerValue.
            // Similarly, when drawing the last sorting layer, include the range from layerValue to short.MaxValue.
            short layerValue = (short)s_SortingLayers[i].value;
            var lowerBound = (i == 0) ? short.MinValue : layerValue;
            var upperBound = (i == s_SortingLayers.Length - 1) ? short.MaxValue : layerValue;
            filterSettings.sortingLayerRange = new SortingLayerRange(lowerBound, upperBound);

            Color clearColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            CoreUtils.SetRenderTarget(cmd, s_GBufferColorTarget.Identifier(), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Color, clearColor);

            cmd.EndSample(sortingLayerName);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Draw the g-buffer.
            DrawingSettings drawSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);

            // Render the lights.
            if (m_GlobalLightMaterial == null)
                m_GlobalLightMaterial = new Material(m_RendererData.globalLightShader);

            if (m_ShapeLightMaterial == null)
                m_ShapeLightMaterial = new Material(m_RendererData.shapeLightShader);

            if (m_PointLightMaterial == null)
                m_PointLightMaterial = new Material(m_RendererData.pointLightShader);

            CoreUtils.SetRenderTarget(cmd, colorAttachment, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None, Color.white);
            var blendStyles = m_RendererData.lightBlendStyles;

            int layerToRender = s_SortingLayers[i].id;
            Light2D.LightStats lightStats = Light2D.GetLightStatsByLayer(layerToRender);

            // global lights
            bool anyGlobalLightDrawn = false;
            for (int j = 0; j < blendStyles.Length; ++j)
            {
                if ((lightStats.blendStylesUsed & (uint)(1 << j)) == 0)
                    continue;

                var lights = Light2D.GetLightsByBlendStyle(j);
                foreach (var light in lights)
                {
                    if (light.lightType != Light2D.LightType.Global || !light.IsLitLayer(layerToRender))
                        continue;

                    cmd.SetGlobalColor("_LightColor", light.intensity * light.color);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_GlobalLightMaterial);
                    anyGlobalLightDrawn = true;
                    break;
                }

                if (anyGlobalLightDrawn)
                    break;
            }

            if (!anyGlobalLightDrawn)
            {
                cmd.SetGlobalColor("_LightColor", Color.black);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_GlobalLightMaterial);
            }

            // non-global lights
            for (int j = 0; j < blendStyles.Length; ++j)
            {
                if ((lightStats.blendStylesUsed & (uint)(1 << j)) == 0)
                    continue;

                string blendStyleName = "Blend Style - " + blendStyles[j].name;
                cmd.BeginSample(blendStyleName);

                var lights = Light2D.GetLightsByBlendStyle(j);
                foreach (var light in lights)
                {
                    if (light == null
                        || light.lightType == Light2D.LightType.Global
                        || !light.IsLitLayer(layerToRender)
                        || !light.IsLightVisible(renderingData.cameraData.camera))
                    {
                        continue;
                    }

                    var lightMesh = light.GetMesh();
                    if (lightMesh == null)
                        continue;

                    cmd.SetGlobalColor("_LightColor", light.intensity * light.color);
                    cmd.SetGlobalTexture("_FalloffLookup", Light2DLookupTexture.CreateFalloffLookupTexture());
                    cmd.SetGlobalFloat("_FalloffIntensity", light.falloffIntensity);
                    cmd.SetGlobalFloat("_FalloffDistance", light.shapeLightFalloffSize);
                    cmd.SetGlobalVector("_FalloffOffset", light.shapeLightFalloffOffset);
                    cmd.SetGlobalFloat("_VolumeOpacity", light.volumeOpacity);
                    cmd.SetGlobalFloat("_HDREmulationScale", m_RendererData.hdrEmulationScale);
                    cmd.SetGlobalFloat("_InverseHDREmulationScale", 1.0f / m_RendererData.hdrEmulationScale);

                    cmd.DisableShaderKeyword("SPRITE_LIGHT");
                    cmd.DisableShaderKeyword("USE_POINT_LIGHT_COOKIES");
                    cmd.EnableShaderKeyword("USE_ADDITIVE_BLENDING");

                    // shape lights
                    if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
                    {
                        if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                        {
                            cmd.EnableShaderKeyword("SPRITE_LIGHT");
                            cmd.SetGlobalTexture("_CookieTex", light.lightCookieSprite.texture);
                        }

                        cmd.DrawMesh(lightMesh, light.transform.localToWorldMatrix, m_ShapeLightMaterial, 0, 1);
                    }
                    else  // point lights
                    {
                        Matrix4x4 lightInverseMatrix;
                        Matrix4x4 lightNoRotInverseMatrix;
                        RendererLighting.GetScaledLightInvMatrix(light, out lightInverseMatrix, true);
                        RendererLighting.GetScaledLightInvMatrix(light, out lightNoRotInverseMatrix, false);

                        float innerRadius = RendererLighting.GetNormalizedInnerRadius(light);
                        float innerAngle = RendererLighting.GetNormalizedAngle(light.pointLightInnerAngle);
                        float outerAngle = RendererLighting.GetNormalizedAngle(light.pointLightOuterAngle);
                        float innerRadiusMult = 1 / (1 - innerRadius);

                        cmd.SetGlobalVector("_LightPosition", light.transform.position);
                        cmd.SetGlobalMatrix("_LightInvMatrix", lightInverseMatrix);
                        cmd.SetGlobalMatrix("_LightNoRotInvMatrix", lightNoRotInverseMatrix);
                        cmd.SetGlobalFloat("_InnerRadiusMult", innerRadiusMult);
                        cmd.SetGlobalFloat("_OuterAngle", outerAngle);
                        cmd.SetGlobalFloat("_InnerAngleMult", 1 / (outerAngle - innerAngle));
                        cmd.SetGlobalTexture("_LightLookup", Light2DLookupTexture.CreatePointLightLookupTexture());
                        cmd.SetGlobalTexture("_FalloffLookup", Light2DLookupTexture.CreateFalloffLookupTexture());
                        cmd.SetGlobalFloat("_FalloffIntensity", light.falloffIntensity);
                        cmd.SetGlobalFloat("_IsFullSpotlight", innerAngle == 1 ? 1.0f : 0.0f);
                        cmd.SetGlobalFloat("_LightZDistance", light.pointLightDistance);

                        if (light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                            cmd.SetGlobalTexture("_PointLightCookieTex", light.lightCookieSprite.texture);

                        if (light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
                            cmd.EnableShaderKeyword("USE_POINT_LIGHT_COOKIES");

                        Vector3 scale = new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius);
                        Matrix4x4 matrix = Matrix4x4.TRS(light.transform.position, Quaternion.identity, scale);
                        cmd.DrawMesh(lightMesh, matrix, m_PointLightMaterial, 0, 1);
                    }
                }

                cmd.EndSample(blendStyleName);
            }
        }

        cmd.ReleaseTemporaryRT(s_GBufferColorTarget.id);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
