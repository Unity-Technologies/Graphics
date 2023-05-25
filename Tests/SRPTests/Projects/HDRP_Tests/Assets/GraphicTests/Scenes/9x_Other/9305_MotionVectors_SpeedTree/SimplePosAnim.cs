using UnityEngine;

public class SimplePosAnim : MonoBehaviour
{
    private int firstFrame;
    private int frame;
    public int loopFrames = 1000;

    public bool pause;
    public bool timeBased;

    private float time;
    public float loopTimeSecs;

    public enum LerpMode
    {
        Linear,
        Smooth,
        Spline
    }
    public LerpMode lerpMode = LerpMode.Linear;
    public float splineTension = 0.5f;
    public Vector3[] animPositions;

    // Start is called before the first frame update
    void Start()
    {
        firstFrame = Time.frameCount; // Ensure frame number is consistent
        frame = 0;
        pause = false;
        timeBased = false;

//#if URP_EXPERIMENTAL_TAA_ENABLE
//        // Reset postprocess history to ensure consistent state.
//        var mainCam = Camera.main;
//        if (mainCam.TryGetComponent<UniversalAdditionalCameraData>(out var data))
//        {
//            data.resetHistory = true;
//        }
//#endif
    }

    int LoopIndex(int index, int length)
    {
        if (length <= 0)
            return index;

        int newIndex = index % length;
        if (newIndex < 0)
            newIndex += length;

        return newIndex;
    }

    Vector3 Spline(Vector3[] points, int index, float t, float tension = 0.5f)
    {
        int len = points.Length;
        int i0 = LoopIndex(index - 1, len);
        int i1 = LoopIndex(index + 0, len);
        int i2 = LoopIndex(index + 1, len);
        int i3 = LoopIndex(index + 2, len);
        Vector3 v0 = points[i0];
        Vector3 v1 = points[i1];
        Vector3 v2 = points[i2];
        Vector3 v3 = points[i3];
        Vector3 s = v1;
        Vector3 sn = (v2 - v0) * tension + s;
        Vector3 e = v2;
        Vector3 en = (v1 - v3) * tension + e;
        Vector3 a0 = Vector3.Lerp(s, sn, t);
        Vector3 a1 = Vector3.Lerp(sn, en, t);
        Vector3 a2 = Vector3.Lerp(en, e, t);
        Vector3 b0 = Vector3.Lerp(a0, a1, t);
        Vector3 b1 = Vector3.Lerp(a1, a2, t);
        Vector3 c0 = Vector3.Lerp(b0, b1, t);
        return c0;
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

        if (animPositions == null || animPositions.Length <= 0)
            return;

        int animPosIndex = (int)(t * animPositions.Length);
        float tFrac = (t * animPositions.Length) - (int)(t * animPositions.Length);

        if (gameObject.TryGetComponent(out Transform objTr))
        {
            switch (lerpMode)
            {
                case LerpMode.Spline:
                    objTr.position = Spline(animPositions, animPosIndex, tFrac, splineTension);
                break;
                case LerpMode.Smooth:
                    tFrac = Mathf.SmoothStep(0.0f, 1.0f, tFrac);
                goto case LerpMode.Linear;
                case LerpMode.Linear:
                default:
                    int animNextPosIndex = LoopIndex(animPosIndex + 1, animPositions.Length);
                    objTr.position = Vector3.Lerp(animPositions[animPosIndex], animPositions[animNextPosIndex], tFrac);
                break;
            }
        }
    }
}
