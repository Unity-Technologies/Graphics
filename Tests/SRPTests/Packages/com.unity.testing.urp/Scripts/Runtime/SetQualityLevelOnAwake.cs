using UnityEngine;

public abstract class SetQualityCallbackObject : MonoBehaviour
{
    public abstract void BeforeChangingQualityLevel(int prevQualityLevelIndex, int newQualityLevelIndex);
    public abstract void BeforeRevertingQualityLevel(int prevQualityLevelIndex, int newQualityLevelIndex);
}

public class SetQualityLevelOnAwake : MonoBehaviour
{
    public int qualityLevelIndex;
    public SetQualityCallbackObject[] callbacks;

    private int prevQualityLevelIndex;
    private string[] qualityLevelNames;

    void Awake()
    {
        InvokeBeforeBeforeChangingCallbacks();

        qualityLevelNames = QualitySettings.names;
        prevQualityLevelIndex = QualitySettings.GetQualityLevel();

        if (qualityLevelIndex >= qualityLevelNames.Length)
        {
            Debug.LogError("SetQualityLevelOnAwake: Quality Level Index " + qualityLevelIndex + " is not available!");
            return;
        }

        /*int curIndex = prevQualityLevelIndex;
        int nextIndex = qualityLevelIndex;
        string cur = qualityLevelNames[prevQualityLevelIndex];
        string next = qualityLevelNames[qualityLevelIndex];
        Debug.Log("SetQualityLevelOnAwake.Awake():\nSwitching from " + cur + "(" + curIndex + ") to " + next + "(" + nextIndex + ")");*/
        QualitySettings.SetQualityLevel(qualityLevelIndex, true);
    }

    void OnDisable()
    {
        InvokeBeforeRevertingCallbacks();

        if (qualityLevelIndex >= qualityLevelNames.Length)
            return;

        /*int curIndex = QualitySettings.GetQualityLevel();
        int nextIndex = prevQualityLevelIndex;
        string cur = qualityLevelNames[curIndex];
        string next = qualityLevelNames[nextIndex];
        Debug.Log("SetQualityLevelOnAwake.OnDisable():\nSwitching from " + cur + "(" + curIndex + ") to " + next + "(" + nextIndex + ")");*/
        QualitySettings.SetQualityLevel(prevQualityLevelIndex, true);
    }

    private void InvokeBeforeBeforeChangingCallbacks()
    {
        if (callbacks == null)
            return;

        for (int i = 0; i < callbacks.Length; i++)
            callbacks[i]?.BeforeChangingQualityLevel(prevQualityLevelIndex, qualityLevelIndex);
    }

    private void InvokeBeforeRevertingCallbacks()
    {
        if (callbacks == null)
            return;

        for (int i = 0; i < callbacks.Length; i++)
            callbacks[i]?.BeforeRevertingQualityLevel(prevQualityLevelIndex, qualityLevelIndex);
    }
}
