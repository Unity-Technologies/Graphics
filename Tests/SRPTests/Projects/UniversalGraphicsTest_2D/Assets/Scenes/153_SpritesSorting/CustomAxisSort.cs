using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CustomAxisSort : MonoBehaviour
{
    public Vector3 customAxis = new Vector3(0f, 1f, 0f);

    // Use this for initialization
    void Start()
    {
        var camera = GetComponent<Camera>();
        camera.transparencySortMode = TransparencySortMode.CustomAxis;
        camera.transparencySortAxis = customAxis;

#if UNITY_EDITOR
        foreach (SceneView sv in SceneView.sceneViews)
        {
            sv.camera.transparencySortMode = TransparencySortMode.CustomAxis;
            sv.camera.transparencySortAxis = customAxis;
        }
#endif
    }
}
