using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TestObjectMotion : MonoBehaviour
{
    public GameObject objCross;

    private int firstFrame;
    private int frame;
    public int loopFrames = 1000;
    public Vector3 objCrossRotation = Vector3.up;

    public bool pause;
    public bool timeBased;

    private float time;
    public float loopTimeSecs;

    // Start is called before the first frame update
    void Start()
    {
        firstFrame = Time.frameCount;
        frame = 0;
        pause = false;
        timeBased = false;

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
        float t = 0;
        if (timeBased)
        {
            if (!pause)
                time += Time.deltaTime;
            if (time > loopTimeSecs)
                time -= loopTimeSecs;
            t = time / Mathf.Max(loopTimeSecs, 0.00001f);
        }
        else
        {
            if (!pause)
                frame = Time.frameCount - firstFrame;
            frame %= Mathf.Max(loopFrames, 1);

            t = frame / (float)loopFrames;
        }

        if (objCross.TryGetComponent(out Transform objTr))
        {
            objTr.rotation = Quaternion.AngleAxis(t * 360.0f, objCrossRotation.normalized);
        }
    }
}
