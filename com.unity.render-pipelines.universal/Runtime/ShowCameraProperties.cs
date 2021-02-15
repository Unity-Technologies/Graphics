using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ShowCameraProperties : MonoBehaviour
{
    public bool isExpended;

    private void OnGUI()
    {
        isExpended = TestCameraProperties.GUIDifference(isExpended);
    }
}
