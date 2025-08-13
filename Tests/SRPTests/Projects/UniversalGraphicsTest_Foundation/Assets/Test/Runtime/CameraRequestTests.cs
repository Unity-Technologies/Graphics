using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

class IssueUIPass
{
    private Camera m_Camera;

    [SetUp]
    public void Setup()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
            Assert.Ignore("URP Only test");

        var go = new GameObject($"TEST_Main");
        m_Camera = go.AddComponent<Camera>();


        // Avoid that the camera renders outside the submit render request
        m_Camera.enabled = false;
    }

    [TearDown]
    public void TearDown()
    {
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
        {
            Object.DestroyImmediate(m_Camera.gameObject);
        }
    }

    // Test for repro Jira Issue in Playmode: https://jira.unity3d.com/browse/UUM-71240
    [UnityTest]
    public IEnumerator FinalBlitSizeTest()
    {
        UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();

        RenderTextureDescriptor desc = new RenderTextureDescriptor(m_Camera.pixelWidth, m_Camera.pixelHeight, RenderTextureFormat.Default, (int)CoreUtils.GetDefaultDepthBufferBits());
        request.destination = RenderTexture.GetTemporary(desc);
        
        // Check if the active render pipeline supports the render request
        if (RenderPipeline.SupportsRenderRequest(m_Camera, request))
        {        
            Assert.DoesNotThrow(()=>RenderPipeline.SubmitRenderRequest(m_Camera, request));
        }
        RenderTexture.ReleaseTemporary(request.destination);

        yield return new WaitForEndOfFrame();
    }
}
