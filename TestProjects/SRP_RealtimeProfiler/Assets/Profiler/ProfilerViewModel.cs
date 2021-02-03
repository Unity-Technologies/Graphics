using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ProfilerViewModel
{
    // Update "data bindings"
    public void UpdateUI(ProfilerDataModel model, VisualElement viewRoot)
    {
        viewRoot.Q<Label>("FullFrameTime").text = $"{model.FrameTime.FullFrameTime:F2}ms";
        viewRoot.Q<Label>("LogicCPUFrameTime").text = $"{model.FrameTime.LogicCPUFrameTime:F2}ms";
        viewRoot.Q<Label>("CombinedCPUFrameTime").text = $"{model.FrameTime.CombinedCPUFrameTime:F2}ms";
        viewRoot.Q<Label>("GPUFrameTime").text = $"{model.FrameTime.GPUFrameTime:F2}ms";
    }
}
