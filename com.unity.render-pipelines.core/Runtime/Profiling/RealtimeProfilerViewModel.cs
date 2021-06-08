using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class RealtimeProfilerViewModel : INotifyPropertyChanged
{
    private int m_SampleHistorySize;
    public int SampleHistorySize
    {
        get => m_SampleHistorySize;
        set
        {
            if (m_SampleHistorySize != value)
            {
                m_SampleHistorySize = value;
                NotifyPropertyChanged();
            }
        }
    }

    private int m_BottleneckHistorySize;
    public int BottleneckHistorySize
    {
        get => m_BottleneckHistorySize;
        set
        {
            if (m_BottleneckHistorySize != value)
            {
                m_BottleneckHistorySize = value;
                NotifyPropertyChanged();
            }
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void Update(VisualElement viewRoot)
    {
        // TODO: This isn't right, should probably use actual bindings (2 views both binding to same value)
        SampleHistorySize = viewRoot.Q<SliderInt>("SampleHistorySize").value;
        BottleneckHistorySize = viewRoot.Q<SliderInt>("BottleneckHistorySize").value;
    }

    // Update "data bindings"
    public void UpdateValues(RealtimeProfilerModel model, VisualElement viewRoot)
    {
        viewRoot.Q<Label>("FullFrameTime").text             = $"{model.AverageSample.FullFrameTime:F2}ms";
        viewRoot.Q<Label>("MainThreadCPUFrameTime").text    = $"{model.AverageSample.MainThreadCPUFrameTime:F2}ms";
        viewRoot.Q<Label>("RenderThreadCPUFrameTime").text  = $"{model.AverageSample.RenderThreadCPUFrameTime:F2}ms";
        viewRoot.Q<Label>("GPUFrameTime").text              = $"{model.AverageSample.GPUFrameTime:F2}ms";

        viewRoot.Q<ProgressBar>("cpu-bound").value = model.BottleneckStats.CPU * 100f;
        viewRoot.Q<ProgressBar>("gpu-bound").value = model.BottleneckStats.GPU * 100f;
        viewRoot.Q<ProgressBar>("present-limited").value = model.BottleneckStats.PresentLimited * 100f;
        viewRoot.Q<ProgressBar>("balanced").value = model.BottleneckStats.Balanced * 100f;
    }

    public void ResetUI(VisualElement viewRoot)
    {
        viewRoot.Q<Label>("FullFrameTime").text             = "0.00ms";
        viewRoot.Q<Label>("MainThreadCPUFrameTime").text    = "0.00ms";
        viewRoot.Q<Label>("RenderThreadCPUFrameTime").text  = "0.00ms";
        viewRoot.Q<Label>("GPUFrameTime").text              = "0.00ms";
    }
}
