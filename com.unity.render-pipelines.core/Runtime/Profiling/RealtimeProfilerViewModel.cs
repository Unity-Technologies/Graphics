using UnityEngine;
using UnityEngine.UIElements;

public class RealtimeProfilerViewModel
{
    // Update "data bindings"
    public void UpdateUI(RealtimeProfilerModel model, VisualElement viewRoot)
    {
        viewRoot.Q<Label>("FullFrameTime").text         = $"{model.FrameTime.FullFrameTime:F2}ms";
        viewRoot.Q<Label>("LogicCPUFrameTime").text     = $"{model.FrameTime.LogicCPUFrameTime:F2}ms";
        viewRoot.Q<Label>("CombinedCPUFrameTime").text  = $"{model.FrameTime.CombinedCPUFrameTime:F2}ms";
        viewRoot.Q<Label>("GPUFrameTime").text          = $"{model.FrameTime.GPUFrameTime:F2}ms";
    }

    public void ResetUI(VisualElement viewRoot)
    {
        viewRoot.Q<Label>("FullFrameTime").text         = "0.00ms";
        viewRoot.Q<Label>("LogicCPUFrameTime").text     = "0.00ms";
        viewRoot.Q<Label>("CombinedCPUFrameTime").text  = "0.00ms";
        viewRoot.Q<Label>("GPUFrameTime").text          = "0.00ms";
    }
}
