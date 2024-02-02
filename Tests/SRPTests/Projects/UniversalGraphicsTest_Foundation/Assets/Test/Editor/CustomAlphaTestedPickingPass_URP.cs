using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// This test loads the scene Test/Editor/CustomAlphaTestedPickingPass/TestScene.unity which contains
/// a camera aligned quad. The quad is rendered with a shader that does alpha testing specifically for
/// the picking pass via a custom function node.
/// This test ensures the node is being included for picking pass generation because it's on the path
/// leading to the BaseColor block, which normally is not used by the picking pass.
/// </summary>
/// <remarks>
/// There is an identical unit test exists in the HDRP_Tests test project to ensure the same behavior
/// on HDRP.
/// </remarks>
class CustomAlphaTestedPickingPass_URP
{
    private SceneView m_SceneView;

    [SetUp]
    public void SetUp()
    {
        m_SceneView = EditorWindow.CreateWindow<SceneView>();
        m_SceneView.position = new Rect(EditorGUIUtility.GetMainWindowPosition().position, new Vector2(512, 512));
        m_SceneView.Focus();
    }

    [TearDown]
    public void TearDown()
    {
        m_SceneView.Close();
        m_SceneView = null;
    }

    [UnityTest]
    public IEnumerator CustomAlphaTestedPickingPassWorks()
    {
        EditorSceneManager.OpenScene("Assets/Test/Editor/CustomAlphaTestedPickingPass/TestScene.unity", OpenSceneMode.Single);
        yield return null;

        float cameraFov = m_SceneView.camera.fieldOfView * Mathf.Deg2Rad;
        float quadSize = 2.0f;

        float cameraDistance = quadSize * 0.5f / Mathf.Tan(cameraFov * 0.5f);
        float sceneViewSize = cameraDistance * Mathf.Sin(cameraFov * 0.5f); // See SceneView.GetPerspectiveCameraDistance
        m_SceneView.LookAt(Vector3.zero, Quaternion.LookRotation(Vector3.forward, Vector3.up), sceneViewSize, false, true);

        Assert.True(SceneView.lastActiveSceneView == m_SceneView);

        // Test the picking hit - the shader is authored so that the bottom-left and top-right quadrant of
        // the quad surface is alpha clipped in the picking pass rendering.

        Assert.True(DoesPickingHit(new Vector2(0.25f, 0.25f)));
        Assert.False(DoesPickingHit(new Vector2(0.25f, 0.75f)));
        Assert.False(DoesPickingHit(new Vector2(0.75f, 0.25f)));
        Assert.True(DoesPickingHit(new Vector2(0.75f, 0.75f)));

        Assert.True(DoesPickingHit(new Vector2(0.45f, 0.45f)));
        Assert.False(DoesPickingHit(new Vector2(0.45f, 0.55f)));
        Assert.False(DoesPickingHit(new Vector2(0.55f, 0.45f)));
        Assert.True(DoesPickingHit(new Vector2(0.55f, 0.55f)));
    }

    private bool DoesPickingHit(Vector2 pos)
    {
        var sceneViewSize = m_SceneView.cameraViewport.size;
        var padding = new Vector2(Mathf.Max(sceneViewSize.x - sceneViewSize.y, 0), Mathf.Max(sceneViewSize.y - sceneViewSize.x, 0));
        padding = padding * 0.5f / sceneViewSize;

        var guiPoint = ((Vector2.one - padding * 2) * pos + padding) * sceneViewSize;
        var overlappingObjects = new List<Object>();

        // GetOverlappingObjects can only be called inside a sceneview gui event.
        System.Action<SceneView> lambda = null;
        lambda = _ =>
        {
            HandleUtility.GetOverlappingObjects(guiPoint, overlappingObjects);
            SceneView.duringSceneGui -= lambda;
        };
        SceneView.duringSceneGui += lambda;
        m_SceneView.SendEvent(EditorGUIUtility.CommandEvent("TestEvent"));

        Assert.IsTrue(overlappingObjects.Count is 0 or 1);
        return overlappingObjects.Count == 1;
    }
}
