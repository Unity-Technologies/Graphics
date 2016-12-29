using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEngine.Rendering;
using UnityEngine.ScriptableRenderPipeline;

[ExecuteInEditMode]
public class RenderLoopTestFixture : RenderPipeline
{
    public delegate void TestDelegate(Camera camera, CullResults cullResults, ScriptableRenderContext renderLoop);
    private static TestDelegate s_Callback;

    private static RenderLoopTestFixture m_Instance;

    [NonSerialized]
    readonly List<Camera> m_CamerasToRender = new List<Camera>();

    public override void Render(ScriptableRenderContext renderLoop, IScriptableRenderDataStore dataStore)
    {
        cameraProvider.GetCamerasToRender(m_CamerasToRender);

        foreach (var camera in m_CamerasToRender)
        {
            if (!camera.enabled)
                continue;
        
            CullingParameters cullingParams;
            bool gotCullingParams = CullResults.GetCullingParameters(camera, out cullingParams);
            Assert.IsTrue(gotCullingParams);

            CullResults cullResults = CullResults.Cull(ref cullingParams, renderLoop);

            if (s_Callback != null)
                s_Callback(camera, cullResults, renderLoop);
        }

        renderLoop.Submit();

        CleanCameras(m_CamerasToRender);
        m_CamerasToRender.Clear();
    }
    
    public static void Run(TestDelegate renderCallback)
    {
        if (m_Instance == null)
        {
            m_Instance = ScriptableObject.CreateInstance<RenderLoopTestFixture>();
        }

        var sceneCamera = Camera.main;
        var camObject = sceneCamera.gameObject;

        GraphicsSettings.renderPipeline = m_Instance;
        s_Callback = renderCallback;
        Transform t = camObject.transform;

        // Can't use AlignViewToObject because it animates over time, and we want the first frame
        float size = SceneView.lastActiveSceneView.size;
        float fov = 90; // hardcoded in SceneView
        float camDist = size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        SceneView.lastActiveSceneView.LookAtDirect(t.position + t.forward * camDist, t.rotation, size);

        sceneCamera.Render();
        GraphicsSettings.renderPipeline = null;
    }
}
