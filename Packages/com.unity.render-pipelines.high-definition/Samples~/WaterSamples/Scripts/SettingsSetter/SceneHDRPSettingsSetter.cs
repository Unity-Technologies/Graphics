using UnityEngine;
using UnityEditor.Rendering.HighDefinition;

[ExecuteInEditMode]
public class SceneHDRPSettingsSetter : MonoBehaviour
{
    #if UNITY_EDITOR
    [SerializeField]
    internal SettingHelperSO settingsHelper;

    void OnEnable()
    {
        SettingsOverlay.settingsSO = settingsHelper;
    }

    void OnDisable()
    {
        SettingsOverlay.settingsSO = null;
    }
#endif
}
