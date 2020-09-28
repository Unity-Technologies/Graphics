using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.TestTools.Graphics; // Don't know why but it doesn't compile when building player

public class SetToCameraNearPlane : MonoBehaviour
{
#if UNITY_EDITOR
    new public MeshRenderer renderer;
    new public Camera camera;
#else
    public MeshRenderer renderer;
    public Camera camera;
#endif

    //public GraphicsTestSettings testSettings;

    [Range(0, 1)]
    public float screenSize = 0.8f;
    public float nearPlaneOffset = 0.001f;

    public Vector2 extend = Vector2.one;

    void Start()
    {
        PlaceObject();
    }

    void PlaceObject ()
    {
        float captureRatio = 1.0f; // testSettings.ImageComparisonSettings.TargetWidth * 1.0f / testSettings.ImageComparisonSettings.TargetHeight;
        float objectRatio = extend.x / extend.y;

        bool scaleBaseOnX = objectRatio >= captureRatio;

        float camDistance = camera.nearClipPlane + nearPlaneOffset;

        float nearPlaneTargetSize = 1f;

        if (camera.orthographic)
        {
            nearPlaneTargetSize = camera.orthographicSize * ((scaleBaseOnX) ? captureRatio : 1f) * screenSize;
        }
        else
        {
            nearPlaneTargetSize = Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad * ((scaleBaseOnX) ? captureRatio : 1f)) * camDistance * screenSize;
        }

        renderer.transform.parent = camera.transform;
        renderer.transform.localPosition = new Vector3(0, 0, camDistance);
        renderer.transform.localRotation = Quaternion.identity;
        renderer.transform.localScale = Vector3.one * Mathf.Abs(nearPlaneTargetSize / ( (scaleBaseOnX) ? extend.x : extend.y ) );

    }

    public bool place = false;
    private void OnValidate()
    {
        if (place)
        {
            place = false;
            PlaceObject();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (renderer == null) return;

        Gizmos.matrix = renderer.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, extend * 2f);
    }
}
