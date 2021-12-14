using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Assertions;

[CreateAssetMenu(fileName = "ScreenCoordOverrideResources", menuName = "Testing/ScreenCoordOverrideResources", order = 1)]
public class ScreenCoordOverrideResources : ScriptableSingleton<ScreenCoordOverrideResources>
{
    const string k_ShaderName = "Hidden/Shader/ScreenCoordPostProcess";

    [SerializeField]
    Shader m_PostProcessingShader;

    void OnValidate()
    {
        if (m_PostProcessingShader == null)
        {
            m_PostProcessingShader = Shader.Find(k_ShaderName);
            Assert.IsNotNull(k_ShaderName, $"Could not find shader \"{k_ShaderName}\".");

            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }
    }

    public Shader PostProcessingShader
    {
        get
        {
            Assert.IsNotNull(m_PostProcessingShader, $"Missing resource \"{nameof(PostProcessingShader)}\"");
            return m_PostProcessingShader;
        }
    }
}
