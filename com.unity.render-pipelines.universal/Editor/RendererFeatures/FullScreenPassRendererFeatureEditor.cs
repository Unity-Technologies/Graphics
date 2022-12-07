using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;

/// <summary>
/// Custom editor for FullScreenPassRendererFeature class responsible for drawing unavailable by default properties
/// such as custom drop down items and additional properties.
/// </summary>
[CustomEditor(typeof(FullScreenPassRendererFeature))]
public class FullScreenPassRendererFeatureEditor : Editor
{
    private FullScreenPassRendererFeature m_AffectedFeature;
    private EditorPrefBool m_ShowAdditionalProperties;
    private int m_PassIndexToUse = 0;

    /// <summary>
    /// A toggle that is responsible whether additional properties are shown.
    /// This toggle also sets pass index to 0 when toggle's value changes.
    /// </summary>
    public bool showAdditionalProperties
    {
        get => m_ShowAdditionalProperties.value;
        set
        {
            if (value != m_ShowAdditionalProperties.value)
            {
                m_PassIndexToUse = 0;
            }
            m_ShowAdditionalProperties.value = value;
        }
    }

    /// <summary>
    /// Implementation for a custom inspector
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawPropertiesExcluding(serializedObject, "m_Script");
        m_AffectedFeature = target as FullScreenPassRendererFeature;

        if (showAdditionalProperties)
        {
            DrawAdditionalProperties();
        }

        m_AffectedFeature.passIndex = m_PassIndexToUse;

        EditorUtility.SetDirty(target);
    }

    private void DrawAdditionalProperties()
    {
        List<string> selectablePasses;
        bool isMaterialValid = m_AffectedFeature.passMaterial != null;
        selectablePasses = isMaterialValid ? GetPassIndexStringEntries(m_AffectedFeature) : new List<string>() {"No material"};

        // If material is invalid 0'th index is selected automatically, so it stays on "No material" entry
        // It is invalid index, but FullScreenPassRendererFeature wont execute until material is valid
        var choiceIndex = EditorGUILayout.Popup("Pass Index", m_AffectedFeature.passIndex, selectablePasses.ToArray());

        m_PassIndexToUse = choiceIndex;

    }

    private List<string> GetPassIndexStringEntries(FullScreenPassRendererFeature component)
    {
        List<string> passIndexEntries = new List<string>();
        for (int i = 0; i < component.passMaterial.passCount; ++i)
        {
            // "Name of a pass (index)" - "PassAlpha (1)"
            string entry = $"{component.passMaterial.GetPassName(i)} ({i})";
            passIndexEntries.Add(entry);
        }

        return passIndexEntries;
    }
}
