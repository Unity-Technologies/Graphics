using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

[TestFixture]
public class MultipleViewGCTest : MonoBehaviour
{
    Recorder m_gcAllocRecorder;
    EditorWindow m_sceneView;

    [OneTimeSetUp]
    public void SetUp()
    {
        //Issue was caused by different nbr of cameras between views
        var mainCamera = Camera.main;
        for(int i = 0; i < 4; ++i)
        {
            var newCam = Instantiate(mainCamera);
        }

        m_sceneView = EditorWindow.GetWindow<SceneView>();

        m_gcAllocRecorder = Recorder.Get("GC.Alloc");
        m_gcAllocRecorder.FilterToCurrentThread();
        m_gcAllocRecorder.enabled = false;

        // Render first frame where gc is ok
        m_sceneView.Repaint();
        Camera.main.Render();
    }

    [Test]
    public void RenderSceneAndGameView()
    {
        Profiler.BeginSample("GC_Alloc_URP_MultipleViews");
        {
            m_gcAllocRecorder.enabled = true;
            m_sceneView.Repaint();
            Camera.main.Render();
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
