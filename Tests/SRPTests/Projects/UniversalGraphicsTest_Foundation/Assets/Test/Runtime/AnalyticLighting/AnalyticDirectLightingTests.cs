using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Direct lighting rendered through the real pipeline and compared against values derived
// from the material model.
[UnityPlatform(exclude = new[] { RuntimePlatform.WebGLPlayer })]
class AnalyticDirectLightingTests
{
    const int k_Size = 64;
    // min16float lets the two inlined evaluations round apart, so allow a few fp16 ULP of the ~0.5 output, well under the 0.12 a defect shows.
    const float k_MaxOverloadDifference = 4e-3f;

    Camera m_Camera;
    Light m_Light;
    GameObject m_Quad;
    RenderTexture m_RenderTexture;
    readonly List<Material> m_Materials = new List<Material>();

    AmbientMode m_SavedAmbientMode;
    Color m_SavedAmbientLight;
    Material m_SavedSkybox;
    bool m_SavedFog;
    float m_SavedReflectionIntensity;
#if UNITY_EDITOR
    bool m_SavedAsyncCompilation;
#endif

    [SetUp]
    public void SetUp()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
            Assert.Ignore("URP only test");
        if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
            Assert.Ignore("This device does not support ARGBFloat render targets.");

#if UNITY_EDITOR
        m_SavedAsyncCompilation = UnityEditor.ShaderUtil.allowAsyncCompilation;
        UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
        m_SavedAmbientMode = RenderSettings.ambientMode;
        m_SavedAmbientLight = RenderSettings.ambientLight;
        m_SavedSkybox = RenderSettings.skybox;
        m_SavedFog = RenderSettings.fog;
        m_SavedReflectionIntensity = RenderSettings.reflectionIntensity;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.reflectionIntensity = 0f;

        var cameraGo = new GameObject("TEST_AnalyticLightingCamera");
        m_Camera = cameraGo.AddComponent<Camera>();
        m_Camera.enabled = false;
        m_Camera.orthographic = true;
        m_Camera.orthographicSize = 0.4f;
        m_Camera.transform.position = new Vector3(0f, 0f, -3f);
        m_Camera.clearFlags = CameraClearFlags.SolidColor;
        m_Camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        var cameraData = m_Camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.None;

        var lightGo = new GameObject("TEST_AnalyticLightingLight");
        m_Light = lightGo.AddComponent<Light>();
        m_Light.type = LightType.Directional;
        m_Light.color = Color.white;
        m_Light.intensity = 1f;
        m_Light.shadows = LightShadows.None;

        m_Quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        m_Quad.name = "TEST_AnalyticLightingQuad";
        Object.DestroyImmediate(m_Quad.GetComponent<Collider>());
        var renderer = m_Quad.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        m_RenderTexture = new RenderTexture(k_Size, k_Size, 24, RenderTextureFormat.ARGBFloat);
        m_RenderTexture.Create();
    }

    [TearDown]
    public void TearDown()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
            return;

        RenderSettings.ambientMode = m_SavedAmbientMode;
        RenderSettings.ambientLight = m_SavedAmbientLight;
        RenderSettings.skybox = m_SavedSkybox;
        RenderSettings.fog = m_SavedFog;
        RenderSettings.reflectionIntensity = m_SavedReflectionIntensity;
#if UNITY_EDITOR
        UnityEditor.ShaderUtil.allowAsyncCompilation = m_SavedAsyncCompilation;
#endif

        foreach (var material in m_Materials)
            Object.DestroyImmediate(material);
        m_Materials.Clear();

        if (m_Quad != null)
            Object.DestroyImmediate(m_Quad);
        if (m_Light != null)
            Object.DestroyImmediate(m_Light.gameObject);
        if (m_Camera != null)
            Object.DestroyImmediate(m_Camera.gameObject);
        if (m_RenderTexture != null)
        {
            m_RenderTexture.Release();
            Object.DestroyImmediate(m_RenderTexture);
        }
    }

    [UnityTest]
    public IEnumerator DeprecatedLightingOverload_MatchesCanonicalOverload(
        [Values(false, true)] bool specularKeywordOff,
        [Values(false, true)] bool specularArgumentOff)
    {
        var material = CreateMaterial(Shader.Find("Hidden/Test/LightingOverloadConsistency"));
        if (specularKeywordOff)
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        material.SetFloat("_SpecularHighlightsOffArg", specularArgumentOff ? 1f : 0f);
        m_Quad.GetComponent<MeshRenderer>().sharedMaterial = material;
        yield return null;

        var pixel = RenderCenterPixel();

        Assert.That(pixel.a, Is.GreaterThan(0.1f), "positive control: the quad must draw a finite canonical value");
        Assert.That(pixel.r, Is.InRange(0f, k_MaxOverloadDifference), "red channel of |canonical - deprecated|");
        Assert.That(pixel.g, Is.InRange(0f, k_MaxOverloadDifference), "green channel of |canonical - deprecated|");
        Assert.That(pixel.b, Is.InRange(0f, k_MaxOverloadDifference), "blue channel of |canonical - deprecated|");
    }

    [UnityTest]
    public IEnumerator DiffuseFollowsCosineFalloff()
    {
        UseLitQuad(smoothness: 0f, specularHighlights: false);
        yield return null;

        var headOn = RenderCenterPixel().g;
        SetLightAngle(30f);
        var at30 = RenderCenterPixel().g;
        SetLightAngle(60f);
        var at60 = RenderCenterPixel().g;

        Assert.That(headOn, Is.GreaterThan(0.05f), "head-on diffuse should be clearly lit");
        Assert.That(at30 / headOn, Is.EqualTo(Mathf.Cos(30f * Mathf.Deg2Rad)).Within(3).Percent, "cos(30) falloff ratio");
        Assert.That(at60 / headOn, Is.EqualTo(Mathf.Cos(60f * Mathf.Deg2Rad)).Within(3).Percent, "cos(60) falloff ratio");
    }

    [UnityTest]
    public IEnumerator DiffuseIsLinearInLightIntensity()
    {
        UseLitQuad(smoothness: 0f, specularHighlights: false);
        yield return null;

        m_Light.intensity = 0.4f;
        var dim = RenderCenterPixel().g;
        m_Light.intensity = 0.8f;
        var bright = RenderCenterPixel().g;

        Assert.That(dim, Is.GreaterThan(0.05f), "dim render should be clearly lit");
        Assert.That(bright / dim, Is.EqualTo(2f).Within(3).Percent, "doubling intensity should double the response");
    }

    [UnityTest]
    public IEnumerator BackfacingLightContributesNothing()
    {
        var albedo = new Color(0.4f, 1f, 0.7f);
        var lightColor = new Color(0.9f, 1f, 0.95f);
        UseLitQuad(smoothness: 0f, specularHighlights: false, albedo, lightColor);
        yield return null;

        var headOn = RenderCenterPixel();
        Assert.That(headOn.r, Is.GreaterThan(0.05f), "positive control red");
        Assert.That(headOn.g, Is.GreaterThan(0.05f), "positive control green");
        Assert.That(headOn.b, Is.GreaterThan(0.05f), "positive control blue");

        SetLightAngle(120f);
        var backface = RenderCenterPixel();
        Assert.That(backface.r, Is.InRange(0f, 0.01f), "backface red");
        Assert.That(backface.g, Is.InRange(0f, 0.01f), "backface green");
        Assert.That(backface.b, Is.InRange(0f, 0.01f), "backface blue");
    }

    [UnityTest]
    public IEnumerator DiffuseMatchesDielectricMaterialModel()
    {
        var albedo = new Color(1f, 0.7f, 0.4f);
        var lightColor = new Color(1f, 0.95f, 0.9f);
        UseLitQuad(smoothness: 0f, specularHighlights: false, albedo, lightColor);
        yield return null;

        var pixel = RenderCenterPixel();

        // With _Metallic 0, linear-space diffuse per channel = albedo * (1 - 0.04) * lightColor (0.04 = kDielectricSpec.r, URP BRDF.hlsl)
        // _BaseColor is an sRGB colour property, so the shader sees albedo.linear.
        var linearAlbedo = albedo.linear;
        var finalLight = FinalLightColor(lightColor, 1f);
        Assert.That(pixel.r, Is.EqualTo(0.96f * linearAlbedo.r * finalLight.r).Within(0.02f), "red");
        Assert.That(pixel.g, Is.EqualTo(0.96f * linearAlbedo.g * finalLight.g).Within(0.02f), "green");
        Assert.That(pixel.b, Is.EqualTo(0.96f * linearAlbedo.b * finalLight.b).Within(0.02f), "blue");
    }

    [UnityTest]
    public IEnumerator SpecularHighlightsAddToDiffuse()
    {
        UseLitQuad(smoothness: 0.5f, specularHighlights: false);
        yield return null;
        var withoutSpecular = RenderCenterPixel().g;

        UseLitQuad(smoothness: 0.5f, specularHighlights: true);
        yield return null;
        var withSpecular = RenderCenterPixel().g;

        Assert.That(withSpecular - withoutSpecular, Is.GreaterThan(0.01f), "mirror-aligned specular should add measurable light");
    }

    [UnityTest]
    public IEnumerator SpecularPeakGrowsWithSmoothness()
    {
        UseLitQuad(smoothness: 0.25f, specularHighlights: true);
        yield return null;
        var rough = RenderCenterPixel().g;

        UseLitQuad(smoothness: 0.7f, specularHighlights: true);
        yield return null;
        var smooth = RenderCenterPixel().g;

        Assert.That(smooth, Is.GreaterThan(rough), "mirror-aligned specular peak should grow with smoothness");
    }

    void UseLitQuad(float smoothness, bool specularHighlights, Color? albedo = null, Color? lightColor = null)
    {
        var material = CreateMaterial(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", albedo ?? Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", smoothness);
        material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        if (!specularHighlights)
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m_Quad.GetComponent<MeshRenderer>().sharedMaterial = material;
        SetLightAngle(0f);
        m_Light.color = lightColor ?? Color.white;
        m_Light.intensity = 1f;
    }

    Material CreateMaterial(Shader shader)
    {
        Assert.That(shader, Is.Not.Null, "test shader must be present in the build");
        var material = new Material(shader);
        m_Materials.Add(material);
        return material;
    }

    void SetLightAngle(float degreesFromNormal)
    {
        m_Light.transform.rotation = Quaternion.Euler(degreesFromNormal, 0f, 0f);
    }

    static Color FinalLightColor(Color color, float intensity)
    {
        return GraphicsSettings.lightsUseLinearIntensity ? color.linear * intensity : (color * intensity).linear;
    }

    Color RenderCenterPixel()
    {
        var request = new UniversalRenderPipeline.SingleCameraRequest { destination = m_RenderTexture };
        RenderPipeline.SubmitRenderRequest(m_Camera, request);

        var readback = AsyncGPUReadback.Request(m_RenderTexture, 0, TextureFormat.RGBAFloat);
        readback.WaitForCompletion();
        Assert.That(readback.hasError, Is.False, "GPU readback failed");

        var pixel = readback.GetData<Vector4>()[(k_Size / 2) * k_Size + k_Size / 2];
        return new Color(pixel.x, pixel.y, pixel.z, pixel.w);
    }
}
