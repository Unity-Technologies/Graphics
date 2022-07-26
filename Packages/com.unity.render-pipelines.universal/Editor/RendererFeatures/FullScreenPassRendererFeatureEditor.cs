using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;

[CustomEditor(typeof(FullScreenPassRendererFeature))]
public class FullScreenPassRendererFeatureEditor : Editor
{
    private FullScreenPassRendererFeature m_AffectedFeature;
    private EditorPrefBool m_ShowAdditionalProperties;
    private int m_PassIndexToUse = 0;

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
