using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;

public class TimeStep : MonoBehaviour
{
    [SerializeField] bool atUpdate = false;
    [SerializeField] int forcedFramesPerSecond = 60;
    [SerializeField] FullScreenDebugMode fullScreenDebugMode = FullScreenDebugMode.None;

    // Use this for initialization
    IEnumerator Start()
    {
        // force to wait for end of frame to allow the renderpipeline to be reset
        yield return new WaitForEndOfFrame();

        if (!atUpdate)
            DoTheThing();
    }

    [ContextMenu("Do The Thing !")]
    void DoTheThing()
    {
        Time.captureFramerate = forcedFramesPerSecond;

        HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        hdPipeline.debugDisplaySettings.fullScreenDebugMode = fullScreenDebugMode;
    }
}
