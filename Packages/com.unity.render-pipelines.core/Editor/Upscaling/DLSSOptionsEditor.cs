#if UNITY_EDITOR
#if ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
using UnityEditor;
using UnityEngine;
using UnityEngine.NVIDIA;

[CustomEditor(typeof(DLSSOptions))]
public class DLSSOptionsEditor : Editor
{
    // Declare variables to hold each property
    private SerializedProperty m_QualityMode;
    private SerializedProperty m_FixedResolution;
    private SerializedProperty m_PresetQuality;
    private SerializedProperty m_PresetBalanced;
    private SerializedProperty m_PresetPerformance;
    private SerializedProperty m_PresetUltraPerformance;
    private SerializedProperty m_PresetDLAA;
    private void OnEnable()
    {
        // Find each property by its exact field name in DLSSOptions.cs
        m_QualityMode = serializedObject.FindProperty("DLSSQualityMode");
        m_FixedResolution = serializedObject.FindProperty("FixedResolutionMode");
        m_PresetQuality = serializedObject.FindProperty("DLSSRenderPresetQuality");
        m_PresetBalanced = serializedObject.FindProperty("DLSSRenderPresetBalanced");
        m_PresetPerformance = serializedObject.FindProperty("DLSSRenderPresetPerformance");
        m_PresetUltraPerformance = serializedObject.FindProperty("DLSSRenderPresetUltraPerformance");
        m_PresetDLAA = serializedObject.FindProperty("DLSSRenderPresetDLAA");
    }

    #region STYLES
    private static readonly string[] DLSSPerfQualityLabels =
    {   // should follow enum value ordering in DLSSQuality enum
        DLSSQuality.MaximumPerformance.ToString(),
        DLSSQuality.Balanced.ToString(),
        DLSSQuality.MaximumQuality.ToString(),
        DLSSQuality.UltraPerformance.ToString(),
        DLSSQuality.DLAA.ToString()
    };
    private static string[][] DLSSPresetOptionsForEachPerfQuality = PopulateDLSSQualityPresetLabels();
    private static string[][] PopulateDLSSQualityPresetLabels()
    {
        int CountBits(uint bitMask) // System.Numerics.BitOperations not available
        {
            int count = 0;
            while (bitMask > 0)
            {
                count += (bitMask & 1) > 0 ? 1 : 0;
                bitMask >>= 1;
            }
            return count;
        }

        System.Array perfQualities = System.Enum.GetValues(typeof(DLSSQuality));
        string[][] labels = new string[perfQualities.Length][];
        foreach (DLSSQuality quality in perfQualities)
        {
            uint presetBitmask = GraphicsDevice.GetAvailableDLSSPresetsForQuality(quality);
            int numPresets = CountBits(presetBitmask) + 1; // +1 for default option which is available to all quality enums
            labels[(int)quality] = new string[numPresets];

            int iWrite = 0;
            System.Array presets = System.Enum.GetValues(typeof(DLSSPreset));
            foreach (DLSSPreset preset in presets)
            {
                if (preset == DLSSPreset.Preset_Default)
                {
                    labels[(int)quality][iWrite++] = "Default Preset";
                    continue;
                }

                if ((presetBitmask & (uint)preset) != 0)
                {
                    string presetName = preset.ToString().Replace('_', ' ');
                    labels[(int)quality][iWrite++] = presetName + " - " + GraphicsDevice.GetDLSSPresetExplanation(preset);
                }
            }
        }
        return labels;
    }

    private static readonly GUIContent renderPresetsLabel = new GUIContent("Render Presets", "Selects an internal DLSS tuning profile. Presets adjust reconstruction behavior, trading off between sharpness, stability, and performance. Different presets may work better depending on scene content and motion.");
    #endregion


    // DLSSOptions contain DLSS presets for each quality mode.
    // The presets available for each quality mode may be different from one another and change over time between DLSS releases.
    // E.g.     DLAAPreset = { F, J, K }
    //      BalancedPreset = { J, K }
    // Instead of letting Unity default-render the GUI, we write our own inspector logic to enforce the preset value requirements.
    void DrawPresetDropdown(ref SerializedProperty presetProp, DLSSQuality perfQuality)
    {
        // each DLSSQuality has a different set of DLSSPresets, represented by a bitmask.
        uint presetBitmask = GraphicsDevice.GetAvailableDLSSPresetsForQuality(perfQuality);
        bool propHasInvalidPresetValue = presetProp.uintValue != 0 && (presetBitmask & presetProp.uintValue) == 0;
        if (propHasInvalidPresetValue)
        {
            Debug.LogWarningFormat("DLSS Preset {0} not found for quality setting {1}, resetting to default value.",
                ((DLSSPreset)presetProp.uintValue).ToString(),
                perfQuality.ToString()
            );
            presetProp.uintValue = 0;
        }

        // We don't want to deal with List<DLSSPreset> & using bitmasks,
        // so we need some bit ops to convert between GUI index <--> Preset value
        int FindPresetGUIIndex(uint presetBitmask, uint presetValue)
        {
            int i = 0;
            while (presetValue > 0)
            {
                i += (presetBitmask & 1) > 0 ? 1 : 0;
                presetBitmask >>= 1;
                presetValue >>= 1;
            }
            return i; // includes 0=default, goes like 1=preset_A, 2=preset_B ...
        }
        uint GUIIndexToPresetValue(uint presetBitmask, uint index)
        {
            // e.g. bitset: 100101 --> 3 bits set, supports 4 presets (0=default, +3 other presets).
            //                   ^ i = 1 -> Preset A = 1
            //                 ^   i = 2 -> Preset C = 4
            //              ^      i = 3 -> Preset F = 32
            uint val = 0;
            while (index > 0 && presetBitmask > 0)
            {
                if ((presetBitmask & 1) != 0)
                    --index;
                presetBitmask >>= 1;
                val = val == 0 ? 1 : (val << 1);
            }
            if (index != 0)
            {
                Debug.LogWarningFormat("DLSSPreset (index={0}) not found in the supported preset list (mask={1}), setting to default value.", index, presetBitmask);
                return 0;
            }
            // Debug.LogFormat("Setting preset {0} : {1}", ((DLSSPreset)val).ToString(), val);
            return val;
        }

        int presetIndex = FindPresetGUIIndex(presetBitmask, presetProp.uintValue);
        int iNew = EditorGUILayout.Popup(DLSSPerfQualityLabels[(int)perfQuality], presetIndex, DLSSPresetOptionsForEachPerfQuality[(int)perfQuality]);
        if (iNew != presetIndex)
            presetProp.uintValue = GUIIndexToPresetValue(presetBitmask, (uint)iNew);
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_QualityMode);
        EditorGUILayout.PropertyField(m_FixedResolution);

        EditorGUILayout.LabelField(renderPresetsLabel, EditorStyles.boldLabel);
        ++EditorGUI.indentLevel;

        DrawPresetDropdown(ref m_PresetQuality, DLSSQuality.MaximumQuality);
        DrawPresetDropdown(ref m_PresetBalanced, DLSSQuality.Balanced);
        DrawPresetDropdown(ref m_PresetPerformance, DLSSQuality.MaximumPerformance);
        DrawPresetDropdown(ref m_PresetUltraPerformance, DLSSQuality.UltraPerformance);
        DrawPresetDropdown(ref m_PresetDLAA, DLSSQuality.DLAA);

        --EditorGUI.indentLevel;

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
#endif
