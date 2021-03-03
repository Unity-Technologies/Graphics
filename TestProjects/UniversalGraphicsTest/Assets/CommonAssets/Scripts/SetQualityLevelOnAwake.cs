using UnityEngine;

public class SetQualityLevelOnAwake : MonoBehaviour
{
    public int qualityLevelIndex;

    private int prevQualityLevelIndex;
    private string[] qualityLevelNames;

    void Awake()
    {
        qualityLevelNames = QualitySettings.names;
        prevQualityLevelIndex = QualitySettings.GetQualityLevel();

        if (qualityLevelIndex >= qualityLevelNames.Length)
        {
            Debug.LogError("SetQualityLevelOnAwake: Quality Level Index " + qualityLevelIndex + " is not available!");
            return;
        }

        int curIndex = prevQualityLevelIndex;
        int nextIndex = qualityLevelIndex;
        string cur = qualityLevelNames[prevQualityLevelIndex];
        string next = qualityLevelNames[qualityLevelIndex];
        //Debug.Log("SetQualityLevelOnAwake.Awake():\nSwitching from " + cur + "(" + curIndex + ") to " + next + "(" + nextIndex + ")");
        QualitySettings.SetQualityLevel(qualityLevelIndex, true);
    }

    void OnDisable()
    {
        if (qualityLevelIndex >= qualityLevelNames.Length)
        {
            return;
        }

        int curIndex = QualitySettings.GetQualityLevel();
        int nextIndex = prevQualityLevelIndex;
        string cur = qualityLevelNames[curIndex];
        string next = qualityLevelNames[nextIndex];
        //Debug.Log("SetQualityLevelOnAwake.OnDisable():\nSwitching from " + cur + "(" + curIndex + ") to " + next + "(" + nextIndex + ")");
        QualitySettings.SetQualityLevel(prevQualityLevelIndex, true);
    }
}
