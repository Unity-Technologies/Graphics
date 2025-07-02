using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[TestFixture]
public class MultipleViewGCTest : MonoBehaviour
{
    Recorder m_gcAllocRecorder;
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

        m_gcAllocRecorder = Recorder.Get("GC.Alloc");
        m_gcAllocRecorder.FilterToCurrentThread();
        m_gcAllocRecorder.enabled = false;

        RenderTextureDescriptor desc = new RenderTextureDescriptor(Camera.main.pixelWidth, Camera.main.pixelHeight, RenderTextureFormat.Default, 32);
        m_RenderTexture = RenderTexture.GetTemporary(desc);

        m_RenderRequest = new UniversalRenderPipeline.SingleCameraRequest { destination = m_RenderTexture };

        // Render first frame where gc is ok
        m_sceneView.Repaint();
        RenderPipeline.SubmitRenderRequest(Camera.main, m_RenderRequest);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        RenderTexture.ReleaseTemporary(m_RenderTexture);
    }

    [Test]
    public void RenderSceneAndGameView()
    {
        Profiler.BeginSample("GC_Alloc_URP_MultipleViews");
        {
            m_gcAllocRecorder.enabled = true;
            m_sceneView.Repaint();
            RenderPipeline.SubmitRenderRequest(Camera.main, m_RenderRequest);
            m_gcAllocRecorder.enabled = false;
        }
        int allocationCountOfRenderPipeline = m_gcAllocRecorder.sampleBlockCount;

        if (allocationCountOfRenderPipeline > 0)
        {
            Debug.LogError($"Memory was allocated {allocationCountOfRenderPipeline} times");
        }
        Profiler.EndSample();
    }
}
