using UnityEngine;
using UnityEngine.Rendering.Universal;


public class ResetHistory : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        var mainCam = Camera.main;
        if (mainCam.TryGetComponent<UniversalAdditionalCameraData>(out var data))
        {
            // Reset Temporal-Antialiasing and other postprocess history for consistent test results.
            data.resetHistory = true;
        }
    }
}
