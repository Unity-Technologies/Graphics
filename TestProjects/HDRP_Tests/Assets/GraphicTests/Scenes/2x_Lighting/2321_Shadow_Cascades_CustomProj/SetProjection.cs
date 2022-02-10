using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetProjection : MonoBehaviour
{
    // Update is called once per frame
    void LateUpdate()
    {
        var camera = GetComponent<Camera>();
        var proj = new Matrix4x4();
        proj.m00 = 1;
        proj.m01 = 0;
        proj.m02 = 0;
        proj.m03 = 0;
        proj.m10 = 0;
        proj.m11 = 1;
        proj.m12 = 0;
        proj.m13 = 0;
        proj.m20 = 0;
        proj.m21 = 0;
        proj.m22 = -1.00000036f;
        proj.m23 = -0.00200000033f;
        proj.m30 = 0;
        proj.m31 = 0;
        proj.m32 = -1;
        proj.m33 = 0;
        camera.projectionMatrix = proj;
    }
}
