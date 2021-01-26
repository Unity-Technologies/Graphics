using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

public class ProfilerViewModel
{
    IMGUIContainer m_GraphContainer = null;
    Texture m_GraphMockup = null;

    // Update "data bindings"
    public void UpdateUI(ProfilerDataModel model, VisualElement viewRoot)
    {
        viewRoot.Q<Label>("CpuFrameTime").text = $"{model.CpuFrameTime:F2}";
        viewRoot.Q<Label>("GpuFrameTime").text = $"{model.GpuFrameTime:F2}";
        
        if (m_GraphContainer == null)
        {
            m_GraphMockup = EditorGUIUtility.Load("Assets/Profiler/graph-mockup.png") as Texture2D;
            m_GraphContainer = viewRoot.Q<IMGUIContainer>("GraphContainer");
            m_GraphContainer.onGUIHandler = () =>
            {
                GUI.DrawTexture(new Rect(0, 0, 200, 100), m_GraphMockup);
                //GUI.Box(new Rect(0, 0, 200, 100), "Test IMGUI content");
            };
        }
    }
}
