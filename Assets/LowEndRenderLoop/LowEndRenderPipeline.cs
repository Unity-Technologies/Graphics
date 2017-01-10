using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.ScriptableRenderLoop;
using UnityEngine.Rendering;
using UnityEngine.ScriptableRenderPipeline;

#region RenderPipelineInstance
public class LowEndRenderPipelineInstance : RenderPipeline
{
    private readonly LowEndRenderPipeline m_Asset;

    ShadowRenderPass m_ShadowPass;
    ShadowSettings m_ShadowSettings = ShadowSettings.Default;

    public LowEndRenderPipelineInstance(LowEndRenderPipeline asset)
    {
        m_Asset = asset;

        BuildShadowSettings();
        m_ShadowPass = new ShadowRenderPass(m_ShadowSettings);
    }

    public override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        base.Render(context, cameras);

        foreach (Camera camera in cameras)
        {
            CullingParameters cullingParameters;
            camera.farClipPlane = 1000.0f;
            if (!CullResults.GetCullingParameters(camera, out cullingParameters))
                continue;

            cullingParameters.shadowDistance = QualitySettings.shadowDistance;
            CullResults cull = CullResults.Cull(ref cullingParameters, context);

            var cmd = new CommandBuffer() { name = "Clear" };
            cmd.ClearRenderTarget(true, false, Color.black);
            context.ExecuteCommandBuffer(cmd);
            cmd.Dispose();

            VisibleLight[] lights = cull.visibleLights;
            ShadowOutput shadowOutput;
            m_ShadowPass.Render(context, cull, out shadowOutput);
            SetupShadowShaderVariables(shadowOutput, context, camera.nearClipPlane, cullingParameters.shadowDistance);

            context.SetupCameraProperties(camera);

            SetupLightShaderVariables(cull.visibleLights, context);
            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName("LowEndForwardBase"));
            settings.sorting.flags = SortFlags.CommonOpaque;
            settings.inputFilter.SetQueuesOpaque();

            if (Lightmapping.bakedGI)
                settings.rendererConfiguration = settings.rendererConfiguration | RendererConfiguration.PerObjectLightmaps;

            context.DrawRenderers(ref settings);
            context.DrawSkybox(camera);

            settings.sorting.flags = SortFlags.CommonTransparent;
            settings.inputFilter.SetQueuesTransparent();
            context.DrawRenderers(ref settings);
        }

        context.Submit();
    }

    private void BuildShadowSettings()
    {
        m_ShadowSettings = ShadowSettings.Default;
        m_ShadowSettings.directionalLightCascadeCount = QualitySettings.shadowCascades;
        m_ShadowSettings.shadowAtlasWidth = m_Asset.ShadowAtlasWidth;
        m_ShadowSettings.shadowAtlasHeight = m_Asset.ShadowAtlasHeight;
        m_ShadowSettings.maxShadowDistance = QualitySettings.shadowDistance;
        m_ShadowSettings.maxShadowLightsSupported = 1;
        m_ShadowSettings.shadowType = ShadowSettings.ShadowType.LIGHTSPACE;
        m_ShadowSettings.renderTextureFormat = RenderTextureFormat.Depth;

        switch (m_ShadowSettings.directionalLightCascadeCount)
        {
            case 1:
                m_ShadowSettings.directionalLightCascades = new Vector3(1.0f, 0.0f, 0.0f);
                break;

            case 2:
                m_ShadowSettings.directionalLightCascades = new Vector3(QualitySettings.shadowCascade2Split, 1.0f, 0.0f);
                break;

            case 4:
                m_ShadowSettings.directionalLightCascades = QualitySettings.shadowCascade4Split;
                break;

            default:
                Debug.LogError("Invalid Shadow Cascade Settings");
                m_ShadowSettings = ShadowSettings.Default;
                break;
        }
    }

    #region HelperMethods
    private void SetupLightShaderVariables(VisibleLight[] lights, ScriptableRenderContext context)
    {
        if (lights.Length <= 0)
            return;

        const int kMaxLights = 8;
        Vector4[] lightPositions = new Vector4[kMaxLights];
        Vector4[] lightColors = new Vector4[kMaxLights];
        Vector4[] lightAttenuations = new Vector4[kMaxLights];
        Vector4[] lightSpotDirections = new Vector4[kMaxLights];

        int pixelLightCount = Mathf.Min(lights.Length, QualitySettings.pixelLightCount);
        int vertexLightCount = (m_Asset.SupportsVertexLight) ? Mathf.Min(lights.Length - pixelLightCount, kMaxLights) : 0;
        int totalLightCount = pixelLightCount + vertexLightCount;

        for (int i = 0; i < totalLightCount; ++i)
        {
            VisibleLight currLight = lights[i];
            if (currLight.lightType == LightType.Directional)
            {
                Vector4 dir = -currLight.localToWorld.GetColumn(2);
                lightPositions[i] = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = currLight.localToWorld.GetColumn(3);
                lightPositions[i] = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            }

            lightColors[i] = currLight.finalColor;

            float rangeSq = currLight.range * currLight.range;
            float quadAtten = (currLight.lightType == LightType.Directional) ? 0.0f : 25.0f / rangeSq;

            if (currLight.lightType == LightType.Spot)
            {
                Vector4 dir = currLight.localToWorld.GetColumn(2);
                lightSpotDirections[i] = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                float spotAngle = Mathf.Deg2Rad * currLight.spotAngle;
                float cosOuterAngle = Mathf.Cos(spotAngle * 0.5f);
                float cosInneAngle = Mathf.Cos(spotAngle * 0.25f);
                float angleRange = cosInneAngle - cosOuterAngle;
                lightAttenuations[i] = new Vector4(cosOuterAngle,
                    Mathf.Approximately(angleRange, 0.0f) ? 1.0f : angleRange, quadAtten, rangeSq);
            }
            else
            {
                lightSpotDirections[i] = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
                lightAttenuations[i] = new Vector4(-1.0f, 1.0f, quadAtten, rangeSq);
            }
        }

        // ambient lighting spherical harmonics values
        const int kSHCoefficients = 7;
        Vector4[] shConstants = new Vector4[kSHCoefficients];
        SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe * RenderSettings.ambientIntensity;
        GetShaderConstantsFromNormalizedSH(ref ambientSH, shConstants);

        CommandBuffer cmd = new CommandBuffer() { name = "SetupShadowShaderConstants" };
        cmd.SetGlobalVectorArray("globalLightPos", lightPositions);
        cmd.SetGlobalVectorArray("globalLightColor", lightColors);
        cmd.SetGlobalVectorArray("globalLightAtten", lightAttenuations);
        cmd.SetGlobalVectorArray("globalLightSpotDir", lightSpotDirections);
        cmd.SetGlobalVector("globalLightCount", new Vector4(pixelLightCount, totalLightCount, 0.0f, 0.0f));

        cmd.SetGlobalVectorArray("globalSH", shConstants);
        context.ExecuteCommandBuffer(cmd);
        cmd.Dispose();
    }

    void SetupShadowShaderVariables(ShadowOutput shadowOutput, ScriptableRenderContext context, float shadowNear, float shadowFar)
    {
        // PSSM distance settings
        float shadowFrustumDepth = shadowFar - shadowNear;
        Vector3 shadowSplitRatio = m_ShadowSettings.directionalLightCascades;

        // We set PSSMDistance to infinity for non active cascades so the comparison test always fails for unavailable cascades
        Vector4 PSSMDistances = new Vector4(
            shadowNear + shadowSplitRatio.x * shadowFrustumDepth,
            (shadowSplitRatio.y > 0.0f) ? shadowNear + shadowSplitRatio.y * shadowFrustumDepth : Mathf.Infinity,
            (shadowSplitRatio.z > 0.0f) ? shadowNear + shadowSplitRatio.z * shadowFrustumDepth : Mathf.Infinity,
            Mathf.Infinity);

        ShadowSliceData[] shadowSlices = shadowOutput.shadowSlices;
        int shadowSliceCount = shadowSlices.Length;

        const int maxShadowCascades = 4;
        Matrix4x4[] shadowMatrices = new Matrix4x4[maxShadowCascades];
        for (int i = 0; i < shadowSliceCount; ++i)
            shadowMatrices[i] = (shadowSliceCount >= i) ? shadowSlices[i].shadowTransform : Matrix4x4.identity;

        var setupShadow = new CommandBuffer() { name = "SetupShadowShaderConstants" };
        setupShadow.SetGlobalMatrixArray("_WorldToShadow", shadowMatrices);
        setupShadow.SetGlobalVector("_PSSMDistances", PSSMDistances);
        context.ExecuteCommandBuffer(setupShadow);
        setupShadow.Dispose();
    }

    private void GetShaderConstantsFromNormalizedSH(ref SphericalHarmonicsL2 ambientProbe, Vector4[] outCoefficients)
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
    #endregion
}
#endregion

public class LowEndRenderPipeline : RenderPipelineAsset
{
#region AssetAndPipelineCreation
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Renderloop/Create Low End Pipeline")]
    static void CreateLowEndPipeline()
    {
        var instance = ScriptableObject.CreateInstance<LowEndRenderPipeline>();
        UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/LowEndRenderLoop/LowEndPipeline.asset");
    }
#endif

    protected override IRenderPipeline InternalCreatePipeline()
    {
        return new LowEndRenderPipelineInstance(this);
    }
#endregion

#region PipelineAssetSettings
    public bool m_SupportsVertexLight = true;
    public int m_ShadowAtlasWidth = 1024;
    public int m_ShadowAtlasHeight = 1024;

    public bool SupportsVertexLight { get { return m_SupportsVertexLight;} private set { m_SupportsVertexLight = value; } }

    public int ShadowAtlasWidth { get { return m_ShadowAtlasWidth; } private set { m_ShadowAtlasWidth = value; } }
    public int ShadowAtlasHeight { get { return m_ShadowAtlasHeight; } private set { m_ShadowAtlasHeight = value; } }
#endregion
}