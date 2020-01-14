using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used in TestProjects/UniversalGraphicsTest/Assets/Scenes/108_MoveCamera.unity
// Test 108 checks that depth buffer is cleared at correct timing
// When depth buffer is incorrectly cleared some pixels of the cube remain blue
public class MoveCamera : MonoBehaviour
{
    void Update()
    {
        transform.RotateAround(Vector3.zero, Vector3.up, 1);
    }
}
