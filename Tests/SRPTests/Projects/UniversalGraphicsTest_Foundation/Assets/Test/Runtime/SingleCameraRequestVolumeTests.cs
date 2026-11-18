using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

class SingleCameraRequestVolumeTests
{
    const int k_LayerA = 30;
    const int k_LayerB = 31;

    Camera m_CameraA;
    Camera m_CameraB;
    VolumeProfile m_ProfileA;
    VolumeProfile m_ProfileB;
    GameObject m_VolumeA;
    GameObject m_VolumeB;

    [SetUp]
    public void Setup()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
            Assert.Ignore("URP Only test");

        m_CameraA = CreateCamera("TEST_CameraA", k_LayerA);
        m_CameraB = CreateCamera("TEST_CameraB", k_LayerB);
        m_VolumeA = CreateGlobalVolume("TEST_VolumeA", k_LayerA, 11f, out m_ProfileA);
        m_VolumeB = CreateGlobalVolume("TEST_VolumeB", k_LayerB, 22f, out m_ProfileB);
    }

    [TearDown]
    public void TearDown()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
            return;

        Object.DestroyImmediate(m_CameraA.gameObject);
        Object.DestroyImmediate(m_CameraB.gameObject);
        Object.DestroyImmediate(m_VolumeA);
        Object.DestroyImmediate(m_VolumeB);
        DestroyProfile(m_ProfileA);
        DestroyProfile(m_ProfileB);
    }

    static void DestroyProfile(VolumeProfile profile)
    {
        foreach (var component in profile.components)
            Object.DestroyImmediate(component);
        Object.DestroyImmediate(profile);
    }

    [UnityTest]
    [Description("A SingleCameraRequest renders with the volume stack of the requested camera, so its shadow settings come from the volumes that camera sees.")]
    public IEnumerator SingleCameraRequest_UsesTheRequestedCameraVolumeStack()
    {
        Assert.AreEqual(11f, RenderAndGetMaxShadowDistance(m_CameraA));
        Assert.AreEqual(22f, RenderAndGetMaxShadowDistance(m_CameraB));
        Assert.AreEqual(11f, RenderAndGetMaxShadowDistance(m_CameraA));

        yield return null;
    }

    static float RenderAndGetMaxShadowDistance(Camera camera)
    {
        var request = new UniversalRenderPipeline.SingleCameraRequest();
        var desc = new RenderTextureDescriptor(64, 64, RenderTextureFormat.Default, (int)CoreUtils.GetDefaultDepthBufferBits());
        request.destination = RenderTexture.GetTemporary(desc);

        Assert.IsTrue(RenderPipeline.SupportsRenderRequest(camera, request));
        RenderPipeline.SubmitRenderRequest(camera, request);
        RenderTexture.ReleaseTemporary(request.destination);

        return VolumeManager.instance.stack.GetComponent<ShadowsVolumeOverride>().maxShadowDistance.value;
    }

    static Camera CreateCamera(string name, int volumeLayer)
    {
        var go = new GameObject(name);
        var camera = go.AddComponent<Camera>();
        camera.enabled = false;
        var additionalData = go.AddComponent<UniversalAdditionalCameraData>();
        additionalData.volumeLayerMask = 1 << volumeLayer;
        return camera;
    }

    static GameObject CreateGlobalVolume(string name, int layer, float maxShadowDistance, out VolumeProfile profile)
    {
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.Add<ShadowsVolumeOverride>().maxShadowDistance.Override(maxShadowDistance);

        var go = new GameObject(name) { layer = layer };
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        return go;
    }
}
