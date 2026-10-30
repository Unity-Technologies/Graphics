using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

[TestFixture]
public class MultipleViewGCTest : MonoBehaviour
{
    EditorWindow m_sceneView;
    RenderTexture m_RenderTexture;
    UniversalRenderPipeline.SingleCameraRequest m_RenderRequest;

    [OneTimeSetUp]
    public void SetUp()
    {
        //Issue was caused by different nbr of cameras between views
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // If main camera is missing, then try to recreate default scene and try again.
            // NOTE: It can be missing if prior test created an empty scene for example. Tests can be dependent on other tests unfortunately :(
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError($"Main camera is missing.");
            }
        }

        for (int i = 0; i < 4; ++i)
        {
            var newCam = Instantiate(mainCamera);
        }

        m_sceneView = EditorWindow.GetWindow<SceneView>();

        RenderTextureDescriptor desc = new RenderTextureDescriptor(Camera.main.pixelWidth, Camera.main.pixelHeight, RenderTextureFormat.Default, 32);
        m_RenderTexture = RenderTexture.GetTemporary(desc);

        m_RenderRequest = new UniversalRenderPipeline.SingleCameraRequest { destination = m_RenderTexture };

        // Render a couple of frames to absorb any transitory GC allocations
        // See https://unity.slack.com/archives/C02LJ5VSV97/p1761922938875599
        const int numFramesToWarmup = 3;

        for (int i = 0; i < numFramesToWarmup; i++)
        {
            m_sceneView.Repaint();
            RenderPipeline.SubmitRenderRequest(Camera.main, m_RenderRequest);
        }
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        RenderTexture.ReleaseTemporary(m_RenderTexture);
    }

    [Test]
    [UnityPlatform(exclude = new RuntimePlatform[] {
        RuntimePlatform.WindowsEditor // Disabled for Instability https://jira.unity3d.com/browse/UUM-125567
    })]
    public void RenderSceneAndGameView()
    {
        Profiler.BeginSample("GC_Alloc_URP_MultipleViews");
        try
        {
            Assert.That(() =>
            {
                m_sceneView.Repaint();
                RenderPipeline.SubmitRenderRequest(Camera.main, m_RenderRequest);
            }, Is.Not.AllocatingGCMemory());
        }
        finally
        {
            Profiler.EndSample();
        }
    }
}
