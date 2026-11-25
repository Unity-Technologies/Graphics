using UnityEngine;

// Particles and trails depend on elapsed time, and the runner renders a variable number of frames before capture
public class FreezeTimeAfterFrames : MonoBehaviour
{
    const int k_CaptureFramerate = 25;
    const int k_FramesBeforeFreeze = 25;

    int m_PreviousCaptureFramerate;
    float m_PreviousTimeScale;
    int m_FrameCount;

    void Awake()
    {
        m_PreviousCaptureFramerate = Time.captureFramerate;
        m_PreviousTimeScale = Time.timeScale;
        Time.captureFramerate = k_CaptureFramerate;
        Time.timeScale = 1.0f;
    }

    void Start()
    {
        const float simulationTime = (float)k_FramesBeforeFreeze / k_CaptureFramerate;
        foreach (var particles in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.InstanceID))
        {
            particles.Simulate(simulationTime, false, true, true);
            particles.Pause(false);
        }
    }

    void Update()
    {
        if (++m_FrameCount == k_FramesBeforeFreeze)
            Time.timeScale = 0.0f;
    }

    void OnDestroy()
    {
        Time.captureFramerate = m_PreviousCaptureFramerate;
        Time.timeScale = m_PreviousTimeScale;
    }
}
