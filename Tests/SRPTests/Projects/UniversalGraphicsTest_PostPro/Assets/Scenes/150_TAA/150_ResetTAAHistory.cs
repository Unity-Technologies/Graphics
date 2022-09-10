using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;


public class ResetTAAHistory : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if URP_EXPERIMENTAL_TAA_ENABLE
        var mainCam = Camera.main;
        if (mainCam.TryGetComponent<UniversalAdditionalCameraData>(out var data))
        {
            data.resetHistory = true;
        }
#endif
    }

    // Update is called once per frame
    void Update()
    {

    }
}
