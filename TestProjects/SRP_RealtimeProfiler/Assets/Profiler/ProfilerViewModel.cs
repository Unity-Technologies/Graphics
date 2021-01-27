using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

public class ProfilerViewModel
{
    // Update "data bindings"
    public void UpdateUI(ProfilerDataModel model, VisualElement viewRoot)
    {
        viewRoot.Q<Label>("CpuMainThreadFrameTime").text = $"{model.CpuFrameTime:F2}ms";
        viewRoot.Q<Label>("GpuFrameTime").text = $"{model.GpuFrameTime:F2}ms";
    }
}
