using UnityEngine;
using UnityEditor.Rendering.HighDefinition;

[ExecuteInEditMode]
public class SceneHDRPSettingsSetter : MonoBehaviour
{
    #if UNITY_EDITOR
    [SerializeField]
    SettingHelperSO settingsHelper;

    void Start()
    {
        SettingsOverlay.instance.settingHelperSO = settingsHelper;
    }
    #endif
}
