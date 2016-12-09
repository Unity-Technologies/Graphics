using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEngine.Experimental.ScriptableRenderLoop;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class RenderLoopTestFixture : RenderPipeline
{
    public delegate void TestDelegate(Camera camera, CullResults cullResults, RenderLoop renderLoop);
    private static TestDelegate s_Callback;

    private static RenderLoopTestFixture m_Instance;

    public override void Render(Camera[] cameras, RenderLoop renderLoop)
    {
        foreach (var camera in cameras)
        {
            CullingParameters cullingParams;
            bool gotCullingParams = CullResults.GetCullingParameters(camera, out cullingParams);
            Assert.IsTrue(gotCullingParams);

            CullResults cullResults = CullResults.Cull(ref cullingParams, renderLoop);

            if (s_Callback != null)
                s_Callback(camera, cullResults, renderLoop);
        }

        renderLoop.Submit();
    }

    public override void Build()
    {
        
    }

    public override void Cleanup()
    {
        
    }

    public static void Run(TestDelegate renderCallback)
    {
        if (m_Instance == null)
        {
            m_Instance = ScriptableObject.CreateInstance<RenderLoopTestFixture>();
        }

        var sceneCamera = Camera.main;
        var camObject = sceneCamera.gameObject;

        GraphicsSettings.SetRenderPipeline(m_Instance);
        s_Callback = renderCallback;
        Transform t = camObject.transform;

        // Can't use AlignViewToObject because it animates over time, and we want the first frame
        float size = SceneView.lastActiveSceneView.size;
        float fov = 90; // hardcoded in SceneView
        float camDist = size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        SceneView.lastActiveSceneView.LookAtDirect(t.position + t.forward * camDist, t.rotation, size);

        sceneCamera.Render();
        GraphicsSettings.SetRenderPipeline(null);
    }
}
