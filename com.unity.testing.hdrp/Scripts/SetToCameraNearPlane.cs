using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetToCameraNearPlane : MonoBehaviour
{
    new public MeshRenderer renderer;
    new public Camera camera;
    [Range(0, 1)]
    public float screenSize = 0.8f;
    public float nearPlaneOffset = 0.001f;

    void Start()
    {
        PlaceObject();
    }

    void PlaceObject ()
    {
        var parent = renderer.transform.parent;

        renderer.transform.parent = null;
        renderer.transform.localPosition = Vector3.zero ;
        renderer.transform.localRotation = Quaternion.identity;
        renderer.transform.localScale = Vector3.one;
        float maxDimension = Mathf.Max(renderer.bounds.extents.x, renderer.bounds.extents.y);

        float camDistance = camera.nearClipPlane + nearPlaneOffset;

        float targetScreenDimension = Mathf.Sin( camera.fieldOfView * Mathf.Max(Screen.height / Screen.width, 1f) * 0.5f * screenSize ) * camDistance;

        renderer.transform.parent = camera.transform;
        renderer.transform.localPosition = new Vector3(0, 0, camDistance);
        renderer.transform.localRotation = Quaternion.identity;
        renderer.transform.localScale = Vector3.one * Mathf.Abs( targetScreenDimension / maxDimension );

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
}
