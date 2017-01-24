using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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

            ShadowOutput shadowOutput;
            m_ShadowPass.Render(context, cull, out shadowOutput);
            SetupShadowShaderVariables(shadowOutput, context, camera.nearClipPlane, cullingParameters.shadowDistance);

            context.SetupCameraProperties(camera);

            SetupLightShaderVariables(cull.visibleLights, context);
            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName("LowEndForwardBase"));
            settings.sorting.flags = SortFlags.CommonOpaque;
            settings.inputFilter.SetQueuesOpaque();

            if (m_Asset.EnableLightmap)
                settings.rendererConfiguration = settings.rendererConfiguration | RendererConfiguration.PerObjectLightmaps;

            if (m_Asset.EnableAmbientProbe)
                settings.rendererConfiguration = settings.rendererConfiguration | RendererConfiguration.PerObjectLightProbe;

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
        m_ShadowSettings.shadowAtlasWidth = m_Asset.ShadowAtlasResolution;
        m_ShadowSettings.shadowAtlasHeight = m_Asset.ShadowAtlasResolution;
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

        CommandBuffer cmd = new CommandBuffer() { name = "SetupShadowShaderConstants" };
        cmd.SetGlobalVectorArray("globalLightPos", lightPositions);
        cmd.SetGlobalVectorArray("globalLightColor", lightColors);
        cmd.SetGlobalVectorArray("globalLightAtten", lightAttenuations);
        cmd.SetGlobalVectorArray("globalLightSpotDir", lightSpotDirections);
        cmd.SetGlobalVector("globalLightCount", new Vector4(pixelLightCount, totalLightCount, 0.0f, 0.0f));
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
        if (shadowSlices == null)
            return;

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
    public bool m_EnableLightmaps = true;
    public bool m_EnableAmbientProbe = true;
    public int m_ShadowAtlasResolution = 1024;

    public bool SupportsVertexLight { get { return m_SupportsVertexLight;} private set { m_SupportsVertexLight = value; } }

    public bool EnableLightmap { get { return m_EnableLightmaps;} private set { m_EnableLightmaps = value; } }

    public bool EnableAmbientProbe { get { return m_EnableAmbientProbe;} private set { m_EnableAmbientProbe = value; } }

    public int ShadowAtlasResolution { get { return m_ShadowAtlasResolution; } private set { m_ShadowAtlasResolution = value; } }
#endregion
}