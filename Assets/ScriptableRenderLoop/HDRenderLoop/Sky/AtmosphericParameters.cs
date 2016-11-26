using UnityEngine;

[ExecuteInEditMode]
public class AtmosphericParameters : MonoBehaviour
{
    public enum OcclusionDownscale { x1 = 1, x2 = 2, x4 = 4 }
    public enum OcclusionSamples   { x64 = 0, x164 = 1, x244 = 2 }
    public enum DepthTexture       { Enable, Disable/*, Ignore*/ } // 'Ignore' appears to be currently unused.
    public enum ScatterDebugMode   { None, Scattering, Occlusion, OccludedScattering, Rayleigh, Mie, Height }

    [Header("Global Settings")]
    public Gradient worldRayleighColorRamp              = null;
    public float    worldRayleighColorIntensity         = 1f;
    public float    worldRayleighDensity                = 10f;
    public float    worldRayleighExtinctionFactor       = 1.1f;
    public float    worldRayleighIndirectScatter        = 0.33f;
    public Gradient worldMieColorRamp                   = null;
    public float    worldMieColorIntensity              = 1f;
    public float    worldMieDensity                     = 15f;
    public float    worldMieExtinctionFactor            = 0f;
    public float    worldMiePhaseAnisotropy             = 0.9f;
    public float    worldNearScatterPush                = 0f;
    public float    worldNormalDistance                 = 1000f;

    [Header("Height Settings")]
    public Color    heightRayleighColor                 = Color.white;
    public float    heightRayleighIntensity             = 1f;
    public float    heightRayleighDensity               = 10f;
    public float    heightMieDensity                    = 0f;
    public float    heightExtinctionFactor              = 1.1f;
    public float    heightSeaLevel                      = 0f;
    public float    heightDistance                      = 50f;
    public Vector3  heightPlaneShift                    = Vector3.zero;
    public float    heightNearScatterPush               = 0f;
    public float    heightNormalDistance                = 1000f;

    [Header("Sky Dome")]
    /*public*/ Vector3        skyDomeScale              = new Vector3(1f, 1f, 1f);
    /*public*/ Vector3        skyDomeRotation           = Vector3.zero;
    /*public*/ Transform      skyDomeTrackedYawRotation = null;
    /*public*/ bool           skyDomeVerticalFlip       = false;
    /*public*/ Cubemap        skyDomeCube               = null;
    /*public*/ float          skyDomeExposure           = 1f;
    /*public*/ Color          skyDomeTint               = Color.white;
    /*public*/ Vector3        skyDomeOffset             = Vector3.zero;

    /*
    [Header("Scatter Occlusion")]
    public bool               useOcclusion              = false;
    public float              occlusionBias             = 0f;
    public float              occlusionBiasIndirect     = 0.6f;
    public float              occlusionBiasClouds       = 0.3f;
    public OcclusionDownscale occlusionDownscale        = OcclusionDownscale.x2;
    public OcclusionSamples   occlusionSamples          = OcclusionSamples.x64;
    public bool               occlusionDepthFixup       = true;
    public float              occlusionDepthThreshold   = 25f;
    public bool               occlusionFullSky          = false;
    public float              occlusionBiasSkyRayleigh  = 0.2f;
    public float              occlusionBiasSkyMie       = 0.4f;
    */
    
    [Header("Other")]
    public Shader             atmosphericShaderOverride = null;
    // public Shader             occlusionShaderOverride   = null;
    public float              worldScaleExponent        = 1.0f;
    // public bool            forcePerPixel             = true; 
    // public bool            forcePostEffect           = true;
    [Tooltip("Soft clouds need depth values. Ignore means externally controlled.")]
    public DepthTexture       depthTexture              = DepthTexture.Enable;
    public ScatterDebugMode   debugMode                 = ScatterDebugMode.None;
    
    // [HideInInspector] public Shader occlusionShaderOverride;

    // Camera   m_currentCamera;

    // UnityEngine.Rendering.CommandBuffer m_occlusionCmdAfterShadows, m_occlusionCmdBeforeScreen;

    Material m_atmosphericMaterial = null;
    // Material m_occlusionMaterial   = null;
    bool     m_isAwake             = false;
    
    public static AtmosphericParameters instance { get; private set; }

    void Awake()
    {
        if (worldRayleighColorRamp == null)
        {
            worldRayleighColorRamp = new Gradient();
            worldRayleighColorRamp.SetKeys(
                new[] { new GradientColorKey(new Color(0.3f, 0.4f, 0.6f), 0f),
                        new GradientColorKey(new Color(0.5f, 0.6f, 0.8f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f) }
            );
        }

        if (worldMieColorRamp == null)
        {
            worldMieColorRamp = new Gradient();
            worldMieColorRamp.SetKeys(
                new[] { new GradientColorKey(new Color(0.95f, 0.75f, 0.5f), 0f),
                        new GradientColorKey(new Color(1f, 0.9f, 8.0f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f) }
            );
        }

        if (atmosphericShaderOverride != null)
        {
            // Override the default shader.
            m_atmosphericMaterial           = new Material(atmosphericShaderOverride);
            m_atmosphericMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        /*
        if (occlusionShaderOverride != null)
        {
            // Override the default shader.
            m_occlusionMaterial           = new Material(occlusionShaderOverride);
            m_occlusionMaterial.hideFlags = HideFlags.HideAndDontSave;
        }
        */

        m_isAwake = true;
    }

    public void OnValidate()
    {
        if (!m_isAwake) return;

        Light light = gameObject.GetComponentInParent<Light>();

        if (light == null || light.type != LightType.Directional)
        {
            Debug.LogErrorFormat("Unexpected: AtmosphericParameters is not attached to a directional light.");
            return;
        }

        worldScaleExponent           = Mathf.Clamp(worldScaleExponent, 1f, 2f);
        worldNormalDistance          = Mathf.Clamp(worldNormalDistance, 1f, 10000f);
        worldNearScatterPush         = Mathf.Clamp(worldNearScatterPush, -200f, 300f);
        worldRayleighDensity         = Mathf.Clamp(worldRayleighDensity, 0, 1000f);
        worldMieDensity              = Mathf.Clamp(worldMieDensity, 0f, 1000f);
        worldRayleighIndirectScatter = Mathf.Clamp(worldRayleighIndirectScatter, 0f, 1f);
        worldMiePhaseAnisotropy      = Mathf.Clamp01(worldMiePhaseAnisotropy);

        heightNormalDistance         = Mathf.Clamp(heightNormalDistance, 1f, 10000f);
        heightNearScatterPush        = Mathf.Clamp(heightNearScatterPush, -200f, 300f);
        heightRayleighDensity        = Mathf.Clamp(heightRayleighDensity, 0, 1000f);
        heightMieDensity             = Mathf.Clamp(heightMieDensity, 0, 1000f);

        /*
        occlusionBias            = Mathf.Clamp01(occlusionBias);
        occlusionBiasIndirect    = Mathf.Clamp01(occlusionBiasIndirect);
        occlusionBiasClouds      = Mathf.Clamp01(occlusionBiasClouds);
        occlusionBiasSkyRayleigh = Mathf.Clamp01(occlusionBiasSkyRayleigh);
        occlusionBiasSkyMie      = Mathf.Clamp01(occlusionBiasSkyMie);
        */
        
        skyDomeExposure = Mathf.Clamp(skyDomeExposure, 0f, 8f);

        if (instance == this)
        {
            // TODO: what's the point of doing this?
            OnDisable();
            OnEnable();
        }

        // TODO: why would I want to do that here?
        // #if UNITY_EDITOR
        //     UnityEditor.SceneView.RepaintAll();
        // #endif
    }

    void OnEnable()
    {
        if (!m_isAwake) return;

        // Define select preprocessor symbols.
        UpdateKeywords(true);

        // Set shader constants.
        UpdateStaticUniforms();

        if (instance && instance != this)
        {
            Debug.LogErrorFormat("Unexpected: AtmosphericParameters.instance already set (to: {0}). Still overriding with: {1}.", instance.name, name);
        }
        
        instance = this;
    }

    void OnDisable()
    {
        // Undefine all preprocessor symbols.
        UpdateKeywords(false);

        if (instance && instance != this)
        {
            Debug.LogErrorFormat("Unexpected: AtmosphericParameters.instance set to: {0}, not to: {1}. Leaving alone.", instance.name, name);
        }
        else
        {
            instance = null;
        }
    }

    void UpdateKeywords(bool enable)
    {
        Shader.DisableKeyword("ATMOSPHERICS_OCCLUSION");
        Shader.DisableKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");
        Shader.DisableKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");
        Shader.DisableKeyword("ATMOSPHERICS_SUNRAYS");
        Shader.DisableKeyword("ATMOSPHERICS_DEBUG");

        if (enable)
        {
            /*
            if (useOcclusion)
            {
                Shader.EnableKeyword("ATMOSPHERICS_OCCLUSION");

                if(occlusionDepthFixup && occlusionDownscale != OcclusionDownscale.x1)
                    Shader.EnableKeyword("ATMOSPHERICS_OCCLUSION_EDGE_FIXUP");

                if(occlusionFullSky)
                    Shader.EnableKeyword("ATMOSPHERICS_OCCLUSION_FULLSKY");
            }
            */

            if (debugMode != ScatterDebugMode.None)
            {
                Shader.EnableKeyword("ATMOSPHERICS_DEBUG");
            }
        }
    }

    void UpdateStaticUniforms()
    {
        Shader.SetGlobalVector("_SkyDomeOffset", skyDomeOffset);
        Shader.SetGlobalVector("_SkyDomeScale", skyDomeScale);
        Shader.SetGlobalTexture("_SkyDomeCube", skyDomeCube);
        Shader.SetGlobalFloat("_SkyDomeExposure", skyDomeExposure);
        Shader.SetGlobalColor("_SkyDomeTint", skyDomeTint);

        /*
        Shader.SetGlobalFloat("_ShadowBias", useOcclusion ? occlusionBias : 1f);
        Shader.SetGlobalFloat("_ShadowBiasIndirect", useOcclusion ? occlusionBiasIndirect : 1f);
        Shader.SetGlobalFloat("_ShadowBiasClouds", useOcclusion ? occlusionBiasClouds : 1f);
        Shader.SetGlobalVector("_ShadowBiasSkyRayleighMie", useOcclusion ? new Vector4(occlusionBiasSkyRayleigh, occlusionBiasSkyMie, 0f, 0f) : Vector4.zero);
        Shader.SetGlobalFloat("_OcclusionDepthThreshold", occlusionDepthThreshold);
        */

        Shader.SetGlobalFloat("_WorldScaleExponent", worldScaleExponent);
        
        Shader.SetGlobalFloat("_WorldNormalDistanceRcp", 1f/worldNormalDistance);
        Shader.SetGlobalFloat("_WorldNearScatterPush", -Mathf.Pow(Mathf.Abs(worldNearScatterPush), worldScaleExponent) * Mathf.Sign(worldNearScatterPush));
        
        Shader.SetGlobalFloat("_WorldRayleighDensity", -worldRayleighDensity / 100000f);
        Shader.SetGlobalFloat("_MiePhaseAnisotropy", worldMiePhaseAnisotropy);
        Shader.SetGlobalVector("_RayleighInScatterPct", new Vector4(1f - worldRayleighIndirectScatter, worldRayleighIndirectScatter, 0f, 0f));
        
        Shader.SetGlobalFloat("_HeightNormalDistanceRcp", 1f/heightNormalDistance);
        Shader.SetGlobalFloat("_HeightNearScatterPush", -Mathf.Pow(Mathf.Abs(heightNearScatterPush), worldScaleExponent) * Mathf.Sign(heightNearScatterPush));
        Shader.SetGlobalFloat("_HeightRayleighDensity", -heightRayleighDensity / 100000f);
        
        Shader.SetGlobalFloat("_HeightSeaLevel", heightSeaLevel);
        Shader.SetGlobalFloat("_HeightDistanceRcp", 1f/heightDistance);
        Shader.SetGlobalVector("_HeightPlaneShift", heightPlaneShift);
        Shader.SetGlobalVector("_HeightRayleighColor", (Vector4)heightRayleighColor * heightRayleighIntensity);
        Shader.SetGlobalFloat("_HeightExtinctionFactor", heightExtinctionFactor);
        Shader.SetGlobalFloat("_RayleighExtinctionFactor", worldRayleighExtinctionFactor);
        Shader.SetGlobalFloat("_MieExtinctionFactor", worldMieExtinctionFactor);
        
        var rayleighColorM20 = worldRayleighColorRamp.Evaluate(0.00f);
        var rayleighColorM10 = worldRayleighColorRamp.Evaluate(0.25f);
        var rayleighColorO00 = worldRayleighColorRamp.Evaluate(0.50f);
        var rayleighColorP10 = worldRayleighColorRamp.Evaluate(0.75f);
        var rayleighColorP20 = worldRayleighColorRamp.Evaluate(1.00f);
        
        var mieColorM20 = worldMieColorRamp.Evaluate(0.00f);
        var mieColorO00 = worldMieColorRamp.Evaluate(0.50f);
        var mieColorP20 = worldMieColorRamp.Evaluate(1.00f);
        
        Shader.SetGlobalVector("_RayleighColorM20", (Vector4)rayleighColorM20 * worldRayleighColorIntensity);
        Shader.SetGlobalVector("_RayleighColorM10", (Vector4)rayleighColorM10 * worldRayleighColorIntensity);
        Shader.SetGlobalVector("_RayleighColorO00", (Vector4)rayleighColorO00 * worldRayleighColorIntensity);
        Shader.SetGlobalVector("_RayleighColorP10", (Vector4)rayleighColorP10 * worldRayleighColorIntensity);
        Shader.SetGlobalVector("_RayleighColorP20", (Vector4)rayleighColorP20 * worldRayleighColorIntensity);
        
        Shader.SetGlobalVector("_MieColorM20", (Vector4)mieColorM20 * worldMieColorIntensity);
        Shader.SetGlobalVector("_MieColorO00", (Vector4)mieColorO00 * worldMieColorIntensity);
        Shader.SetGlobalVector("_MieColorP20", (Vector4)mieColorP20 * worldMieColorIntensity);

        Shader.SetGlobalFloat("_AtmosphericsDebugMode", (int)debugMode);
    }

    void OnWillRenderObject()
    {
        if (!m_isAwake) return;

        /* For now, we only use the directional light we are attached to, and the current camera. */

        if (depthTexture == DepthTexture.Disable)
        {
            // Disable depth texture rendering.
            Camera.current.depthTextureMode = DepthTextureMode.None;
        }

        // Set shader constants.
        UpdateDynamicUniforms();

        /*
        if(useOcclusion) {
            var camRgt = m_currentCamera.transform.right;
            var camUp = m_currentCamera.transform.up;
            var camFwd = m_currentCamera.transform.forward;
                
            var dy = Mathf.Tan(m_currentCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var dx = dy * m_currentCamera.aspect;
                
            var vpCenter = camFwd * m_currentCamera.farClipPlane;
            var vpRight = camRgt * dx * m_currentCamera.farClipPlane;
            var vpUp = camUp * dy * m_currentCamera.farClipPlane;

            m_occlusionMaterial.SetVector("_CameraPosition", m_currentCamera.transform.position);
            m_occlusionMaterial.SetVector("_ViewportCorner", vpCenter - vpRight - vpUp);
            m_occlusionMaterial.SetVector("_ViewportRight", vpRight * 2f);
            m_occlusionMaterial.SetVector("_ViewportUp", vpUp * 2f);
            var farDist = m_currentCamera ? m_currentCamera.farClipPlane : 1000f;
            var refDist = (Mathf.Min(farDist, QualitySettings.shadowDistance) - 1f) / farDist;
            m_occlusionMaterial.SetFloat("_OcclusionSkyRefDistance", refDist);

            var srcRect = m_currentCamera.pixelRect;
            var downscale = 1f / (float)(int)occlusionDownscale;
            var occWidth = Mathf.RoundToInt(srcRect.width * downscale);
            var occHeight = Mathf.RoundToInt(srcRect.height * downscale);
            var occlusionId = Shader.PropertyToID("_OcclusionTexture");    

            m_occlusionCmdBeforeScreen.Clear();
            m_occlusionCmdBeforeScreen.GetTemporaryRT(occlusionId, occWidth, occHeight, 0, FilterMode.Bilinear, RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);
            m_occlusionCmdBeforeScreen.Blit(
                null, 
                occlusionId,
                m_occlusionMaterial,
                (int)occlusionSamples
            );
            m_occlusionCmdBeforeScreen.SetGlobalTexture(occlusionId, occlusionId);
        }
        */
    }

    void UpdateDynamicUniforms()
    {
        var trackedYaw = skyDomeTrackedYawRotation ? skyDomeTrackedYawRotation.eulerAngles.y : 0f;
        Shader.SetGlobalMatrix("_SkyDomeRotation",
             Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(skyDomeRotation.x, 0f, 0f), Vector3.one)
           * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, skyDomeRotation.y - trackedYaw, 0f), Vector3.one)
           * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, skyDomeVerticalFlip ? -1f : 1f, 1f))                   
        );

        Shader.SetGlobalVector("_SunDirection",    transform.forward); 
        Shader.SetGlobalFloat("_WorldMieDensity",  -worldMieDensity / 100000f);
        Shader.SetGlobalFloat("_HeightMieDensity", -heightMieDensity / 100000f);

        var pixelRect = Camera.current ? Camera.current.pixelRect
                                       : new Rect(0f, 0f, Screen.width, Screen.height);
        var scale = 1.0f; //(float)(int)occlusionDownscale;
        var depthTextureScaledTexelSize = new Vector4(scale / pixelRect.width,
                                                      scale / pixelRect.height,
                                                     -scale / pixelRect.width,
                                                     -scale / pixelRect.height);
        Shader.SetGlobalVector("_DepthTextureScaledTexelSize", depthTextureScaledTexelSize);
    }

    void OnRenderObject()
    {
        /* This component is not rendered directly. */
    }
}
