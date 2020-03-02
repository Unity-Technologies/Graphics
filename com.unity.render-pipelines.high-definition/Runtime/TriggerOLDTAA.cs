using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class TriggerOLDTAA : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var hdAdditional = GetComponent<HDAdditionalCameraData>();

        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            hdAdditional.oldTAA = !hdAdditional.oldTAA;
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            if(hdAdditional.antialiasing != HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing)
                hdAdditional.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            else
                hdAdditional.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {

            if (hdAdditional.taaMotionVectorRejection > 0.4)
            {
                hdAdditional.taaMotionVectorRejection = 0;
            }
            else
            {
                hdAdditional.taaMotionVectorRejection = 0.5f;
            }
        }

        if (Input.GetKeyDown(KeyCode.E))
        {

            if (hdAdditional.TAAQuality == HDAdditionalCameraData.TAAQualityLevel.Medium)
            {
                hdAdditional.TAAQuality = HDAdditionalCameraData.TAAQualityLevel.High;
            }
            else
            {
                hdAdditional.TAAQuality = HDAdditionalCameraData.TAAQualityLevel.Medium;
            }
        }
    }
}
