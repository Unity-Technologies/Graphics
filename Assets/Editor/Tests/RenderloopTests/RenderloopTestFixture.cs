using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using NUnit.Framework;

[ExecuteInEditMode]
public class RenderLoopTestFixture : MonoBehaviour
{
    public delegate void TestDelegate(Camera camera, CullResults cullResults, RenderLoop renderLoop);
    private static TestDelegate callback;

    public static void Render(RenderLoopWrapper wrapper, Camera[] cameras, RenderLoop renderLoop)
    {
        foreach (Camera camera in cameras)
        {
            CullingParameters cullingParams;
            bool gotCullingParams = CullResults.GetCullingParameters(camera, out cullingParams);
            Assert.IsTrue(gotCullingParams);

            CullResults cullResults = CullResults.Cull(ref cullingParams, renderLoop);

            callback(camera, cullResults, renderLoop);
        }

        renderLoop.Submit();
    }

    public static void Run(TestDelegate renderCallback)
    {
        var sceneCamera = Camera.main;
        var camObject = sceneCamera.gameObject;

        var instance = camObject.AddComponent<RenderLoopWrapper>();
        instance.callback = Render;
        callback = renderCallback;
        instance.enabled = true;

        Transform t = camObject.transform;

        // Can't use AlignViewToObject because it animates over time, and we want the first frame
        float size = SceneView.lastActiveSceneView.size;
        float fov = 90; // hardcoded in SceneView
        float camDist = size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        SceneView.lastActiveSceneView.LookAtDirect(t.position + t.forward * camDist, t.rotation, size);

        // Invoke renderer
        try
        {
            sceneCamera.Render();
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
