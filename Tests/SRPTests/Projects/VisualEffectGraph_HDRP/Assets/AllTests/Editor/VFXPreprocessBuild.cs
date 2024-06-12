using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class VFXPreprocessBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => int.MinValue + 49;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("Manually excluding Switch to workaround UUM-73363");
        QualitySettings.TryExcludePlatformAt("Switch", 1, out var _);
    }
}
