using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SRPBatchSetting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }
}
