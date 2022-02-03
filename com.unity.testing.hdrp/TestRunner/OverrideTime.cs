using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Utility for achieving deterministic motion vector results with vertex animation in play mode.
public class OverrideTime : MonoBehaviour
{
    public Camera m_Camera;
    private HDCamera m_CameraHD;

    public float m_Time = 1f;
    public float m_DeltaTime = 0.033f;

    void SetOverrideTime(float ct, float lt)
    {
        if (m_CameraHD == null && m_Camera != null)
            m_CameraHD = HDCamera.GetOrCreate(m_Camera);

        m_CameraHD.animateMaterialsTime = ct;
        m_CameraHD.animateMaterialsTimeLast = lt;
    }

    private void OnEnable()
    {
        SetOverrideTime(m_Time, m_Time - m_DeltaTime);
    }

    private void Update()
    {
        SetOverrideTime(m_Time, m_Time - m_DeltaTime);
    }

    private void OnDisable()
    {
        SetOverrideTime(-1, -1);
    }
}
