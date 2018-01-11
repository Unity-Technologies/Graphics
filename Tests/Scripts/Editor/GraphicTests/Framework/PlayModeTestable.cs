using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Events;
using System.Collections;

public class PlayModeTestable : SetupSceneForRenderPipelineTest, IMonoBehaviourTest
{
    float t = 0f;
    public float testDuration = 1f;

    public bool IsTestFinished
    {
        get
        {
            t += Time.deltaTime;
            return (t > testDuration);
        }
    }
}
