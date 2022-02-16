using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Assertions;

public class ScreenCoordOverrideResources : MonoBehaviour
{
    static ScreenCoordOverrideResources s_Instance;

    public static ScreenCoordOverrideResources GetInstance()
    {
        if (s_Instance == null)
        {
            s_Instance = FindObjectOfType<ScreenCoordOverrideResources>();
            Assert.IsNotNull(s_Instance, $"Could not find instance of \"{nameof(ScreenCoordOverrideResources)}\".");
        }

        return s_Instance;
    }

    const string k_ShaderName = "Hidden/Test/ScreenCoordOverrideFullScreen";

    [SerializeField]
    Shader m_FullScreenShader;

    void OnValidate()
    {
        if (m_FullScreenShader == null)
        {
            m_FullScreenShader = Shader.Find(k_ShaderName);
            Assert.IsNotNull(k_ShaderName, $"Could not find shader \"{k_ShaderName}\".");

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }

    public Shader FullScreenShader
    {
        get
        {
            Assert.IsNotNull(m_FullScreenShader, $"Missing resource \"{nameof(FullScreenShader)}\"");
            return m_FullScreenShader;
        }
    }
}
