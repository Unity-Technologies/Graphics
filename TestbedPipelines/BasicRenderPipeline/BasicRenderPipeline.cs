using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.XR;

// Very basic scriptable rendering loop example:
// - Use with BasicRenderPipelineShader.shader (the loop expects "BasicPass" pass type to exist)
// - Supports up to 8 enabled lights in the scene (directional, point or spot)
// - Does the same physically based BRDF as the Standard shader
// - No shadows
// - This loop also does not setup lightmaps, light probes, reflection probes or light cookies

[ExecuteInEditMode]
public class BasicRenderPipeline : RenderPipelineAsset
{
    public bool UseIntermediateRenderTargetBlit;

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Create/Graphics/Basic Render Pipeline", priority = CoreUtils.assetCreateMenuPriority1)]
    static void CreateBasicRenderPipeline()
    {
        var instance = ScriptableObject.CreateInstance<BasicRenderPipeline>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/BasicRenderPipelineTutorial/BasicRenderPipeline.asset");
    }

#endif

    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new BasicRenderPipelineInstance(UseIntermediateRenderTargetBlit);
    }
}

public class BasicRenderPipelineInstance : RenderPipeline
{
    bool useIntermediateBlit;

    public BasicRenderPipelineInstance()
    {
        useIntermediateBlit = false;
    }

    public BasicRenderPipelineInstance(bool useIntermediate)
    {
        useIntermediateBlit = useIntermediate;
    }

    public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        base.Render(renderContext, cameras);
        BasicRendering.Render(renderContext, cameras, useIntermediateBlit);
    }
}

public static class BasicRendering
{
    static void ConfigureAndBindIntermediateRenderTarget(ScriptableRenderContext context, Camera cam, bool stereoEnabled, out RenderTargetIdentifier intermediateRTID, out bool isRTTexArray)
    {
        var intermediateRT = Shader.PropertyToID("_IntermediateTarget");
        intermediateRTID = new RenderTargetIdentifier(intermediateRT);

        isRTTexArray = false;

        var bindIntermediateRTCmd = CommandBufferPool.Get("Bind intermediate RT");

        if (stereoEnabled)
        {
            RenderTextureDescriptor xrDesc = XRSettings.eyeTextureDesc;
            xrDesc.depthBufferBits = 24;

            if (xrDesc.dimension == TextureDimension.Tex2DArray)
                isRTTexArray = true;

            bindIntermediateRTCmd.GetTemporaryRT(intermediateRT, xrDesc, FilterMode.Point);
        }
        else
        {
            int w = cam.pixelWidth;
            int h = cam.pixelHeight;
            bindIntermediateRTCmd.GetTemporaryRT(intermediateRT, w, h, 24, FilterMode.Point, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1, true);
        }

        if (isRTTexArray)
            bindIntermediateRTCmd.SetRenderTarget(intermediateRTID, 0, CubemapFace.Unknown, -1); // depthSlice == -1 => bind all slices
        else
            bindIntermediateRTCmd.SetRenderTarget(intermediateRTID);

        context.ExecuteCommandBuffer(bindIntermediateRTCmd);
        CommandBufferPool.Release(bindIntermediateRTCmd);
    }

    static void BlitFromIntermediateToCameraTarget(ScriptableRenderContext context, RenderTargetIdentifier intermediateRTID, bool isRTTexArray)
    {
        var blitIntermediateRTCmd = CommandBufferPool.Get("Copy intermediate RT to default RT");

        if (isRTTexArray)
        {
            // Currently, Blit does not allow specification of a slice in a texture array.
            // It can use the CurrentActive render texture's bound slices, so we use that
            // as a temporary workaround.
            blitIntermediateRTCmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown, -1);
            blitIntermediateRTCmd.Blit(intermediateRTID, BuiltinRenderTextureType.CurrentActive);
        }
        else
            blitIntermediateRTCmd.Blit(intermediateRTID, BuiltinRenderTextureType.CameraTarget);

        context.ExecuteCommandBuffer(blitIntermediateRTCmd);
        CommandBufferPool.Release(blitIntermediateRTCmd);

    }

    // Main entry point for our scriptable render loop

    public static void Render(ScriptableRenderContext context, IEnumerable<Camera> cameras, bool useIntermediateBlitPath)
    {
        bool stereoEnabled = XRSettings.isDeviceActive;

        foreach (var camera in cameras)
        {
            // Culling
            ScriptableCullingParameters cullingParams;
            // Stereo-aware culling parameters are configured to perform a single cull for both eyes
            if (!CullResults.GetCullingParameters(camera, stereoEnabled, out cullingParams))
                continue;
            CullResults cull = new CullResults();
            CullResults.Cull(ref cullingParams, context, ref cull);

            // Setup camera for rendering (sets render target, view/projection matrices and other
            // per-camera built-in shader variables).
            // If stereo is enabled, we also configure stereo matrices, viewports, and XR device render targets
            context.SetupCameraProperties(camera, stereoEnabled);

            // Draws in-between [Start|Stop]MultiEye are stereo-ized by engine
            if (stereoEnabled)
                context.StartMultiEye(camera);

            RenderTargetIdentifier intermediateRTID = new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive);
            bool isIntermediateRTTexArray = false;
            if (useIntermediateBlitPath)
            {
                ConfigureAndBindIntermediateRenderTarget(context, camera, stereoEnabled, out intermediateRTID, out isIntermediateRTTexArray);
            }

            // clear depth buffer
            var cmd = CommandBufferPool.Get();
            cmd.ClearRenderTarget(true, false, Color.black);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // Setup global lighting shader variables
            SetupLightShaderVariables(cull.visibleLights, context);

            // Draw opaque objects using BasicPass shader pass
            var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("BasicPass")) { sorting = { flags = SortFlags.CommonOpaque } };
            var filterSettings = new FilterRenderersSettings(true) { renderQueueRange = RenderQueueRange.opaque };
            context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

            // Draw skybox
            context.DrawSkybox(camera);

            // Draw transparent objects using BasicPass shader pass
            drawSettings.sorting.flags = SortFlags.CommonTransparent;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull.visibleRenderers, ref drawSettings, filterSettings);

            if (useIntermediateBlitPath)
            {
                BlitFromIntermediateToCameraTarget(context, intermediateRTID, isIntermediateRTTexArray);
            }

            if (stereoEnabled)
            {
                context.StopMultiEye(camera);
                // StereoEndRender will reset state on the camera to pre-Stereo settings,
                // and invoke XR based events/callbacks.
                context.StereoEndRender(camera);
            }

            context.Submit();
        }
    }

    // Setup lighting variables for shader to use

    private static void SetupLightShaderVariables(List<VisibleLight> lights, ScriptableRenderContext context)
    {
        // We only support up to 8 visible lights here. More complex approaches would
        // be doing some sort of per-object light setups, but here we go for simplest possible
        // approach.
        const int kMaxLights = 8;
        // Just take first 8 lights. Possible improvements: sort lights by intensity or distance
        // to the viewer, so that "most important" lights in the scene are picked, and not the 8
        // that happened to be first.
        int lightCount = Mathf.Min(lights.Count, kMaxLights);

        // Prepare light data
        Vector4[] lightColors = new Vector4[kMaxLights];
        Vector4[] lightPositions = new Vector4[kMaxLights];
        Vector4[] lightSpotDirections = new Vector4[kMaxLights];
        Vector4[] lightAtten = new Vector4[kMaxLights];
        for (var i = 0; i < lightCount; ++i)
        {
            VisibleLight light = lights[i];
            lightColors[i] = light.finalColor;
            if (light.lightType == LightType.Directional)
            {
                // light position for directional lights is: (-direction, 0)
                var dir = light.localToWorld.GetColumn(2);
                lightPositions[i] = new Vector4(-dir.x, -dir.y, -dir.z, 0);
            }
            else
            {
                // light position for point/spot lights is: (position, 1)
                var pos = light.localToWorld.GetColumn(3);
                lightPositions[i] = new Vector4(pos.x, pos.y, pos.z, 1);
            }
            // attenuation set in a way where distance attenuation can be computed:
            //  float lengthSq = dot(toLight, toLight);
            //  float atten = 1.0 / (1.0 + lengthSq * LightAtten[i].z);
            // and spot cone attenuation:
            //  float rho = max (0, dot(normalize(toLight), SpotDirection[i].xyz));
            //  float spotAtt = (rho - LightAtten[i].x) * LightAtten[i].y;
            //  spotAtt = saturate(spotAtt);
            // and the above works for all light types, i.e. spot light code works out
            // to correct math for point & directional lights as well.

            float rangeSq = light.range * light.range;

            float quadAtten = (light.lightType == LightType.Directional) ? 0.0f : 25.0f / rangeSq;

            // spot direction & attenuation
            if (light.lightType == LightType.Spot)
            {
                var dir = light.localToWorld.GetColumn(2);
                lightSpotDirections[i] = new Vector4(-dir.x, -dir.y, -dir.z, 0);

                float radAngle = Mathf.Deg2Rad * light.spotAngle;
                float cosTheta = Mathf.Cos(radAngle * 0.25f);
                float cosPhi = Mathf.Cos(radAngle * 0.5f);
                float cosDiff = cosTheta - cosPhi;
                lightAtten[i] = new Vector4(cosPhi, (cosDiff != 0.0f) ? 1.0f / cosDiff : 1.0f, quadAtten, rangeSq);
            }
            else
            {
                // non-spot light
                lightSpotDirections[i] = new Vector4(0, 0, 1, 0);
                lightAtten[i] = new Vector4(-1, 1, quadAtten, rangeSq);
            }
        }

        // ambient lighting spherical harmonics values
        const int kSHCoefficients = 7;
        Vector4[] shConstants = new Vector4[kSHCoefficients];
        SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe * RenderSettings.ambientIntensity;
        GetShaderConstantsFromNormalizedSH(ref ambientSH, shConstants);

        // setup global shader variables to contain all the data computed above
        CommandBuffer cmd = CommandBufferPool.Get();
        cmd.SetGlobalVectorArray("globalLightColor", lightColors);
        cmd.SetGlobalVectorArray("globalLightPos", lightPositions);
        cmd.SetGlobalVectorArray("globalLightSpotDir", lightSpotDirections);
        cmd.SetGlobalVectorArray("globalLightAtten", lightAtten);
        cmd.SetGlobalVector("globalLightCount", new Vector4(lightCount, 0, 0, 0));
        cmd.SetGlobalVectorArray("globalSH", shConstants);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    // Prepare L2 spherical harmonics values for efficient evaluation in a shader

    private static void GetShaderConstantsFromNormalizedSH(ref SphericalHarmonicsL2 ambientProbe, Vector4[] outCoefficients)
    {
        for (int channelIdx = 0; channelIdx < 3; ++channelIdx)
        {
            // Constant + Linear
            // In the shader we multiply the normal is not swizzled, so it's normal.xyz.
            // Swizzle the coefficients to be in { x, y, z, DC } order.
            outCoefficients[channelIdx].x = ambientProbe[channelIdx, 3];
            outCoefficients[channelIdx].y = ambientProbe[channelIdx, 1];
            outCoefficients[channelIdx].z = ambientProbe[channelIdx, 2];
            outCoefficients[channelIdx].w = ambientProbe[channelIdx, 0] - ambientProbe[channelIdx, 6];
            // Quadratic polynomials
            outCoefficients[channelIdx + 3].x = ambientProbe[channelIdx, 4];
            outCoefficients[channelIdx + 3].y = ambientProbe[channelIdx, 5];
            outCoefficients[channelIdx + 3].z = ambientProbe[channelIdx, 6] * 3.0f;
            outCoefficients[channelIdx + 3].w = ambientProbe[channelIdx, 7];
        }
        // Final quadratic polynomial
        outCoefficients[6].x = ambientProbe[0, 8];
        outCoefficients[6].y = ambientProbe[1, 8];
        outCoefficients[6].z = ambientProbe[2, 8];
        outCoefficients[6].w = 1.0f;
    }
}
