using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ShowCameraProperties : MonoBehaviour
{
    public bool isExpended;

    private void OnGUI()
    {
        TestCameraProperties.isActive = true;
        isExpended = TestCameraProperties.GUIDifference(isExpended);
    }
}
