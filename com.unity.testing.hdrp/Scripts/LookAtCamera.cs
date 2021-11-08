using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public Camera targetCamera;
    public Vector3 additionalRotation = new Vector3(0f, 180f, 0f);

    [ContextMenu("Look At")]
    public void LookAt()
    {
        if (targetCamera == null && Camera.main == null) return;

        transform.rotation = Quaternion.LookRotation(((targetCamera == null) ? Camera.main.transform.position : targetCamera.transform.position) - transform.position) * Quaternion.Euler(additionalRotation);
    }
}
