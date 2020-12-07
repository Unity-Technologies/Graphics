using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class ForwardLoader : MonoBehaviour
{
    public RenderPipelineAsset RenderPipelineOverride;

    // Loading the scene in start didn't seem to work so we roll our own sequencing
    int updateCount = 0;

    // Update is called once per frame
    void Update()
    {
        if (updateCount == 0)
        {
            updateCount = 1;
            //Debug.Log("Scene Loading Frame");
            // This only happens at the end of the frame or something
            SceneManager.LoadScene("Scenes/003-VirtualTexturing", LoadSceneMode.Additive);
        }
        else if (updateCount == 1)
        {
            // Another frame, another try, scene is now loaded so set it up
            updateCount = 2;

            GraphicsSettings.renderPipelineAsset = RenderPipelineOverride;
        }
    }
}
