using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class RealtimeProfilerViewModel : INotifyPropertyChanged
{
    private int m_HistorySize;
    public int HistorySize
    {
        get => m_HistorySize;
        set
        {
            if (m_HistorySize != value)
            {
                m_HistorySize = value;
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
        HistorySize = viewRoot.Q<SliderInt>("HistorySize").value;
    }

    // Update "data bindings"
    public void UpdateValues(RealtimeProfilerModel model, VisualElement viewRoot)
    {
        viewRoot.Q<Label>("FullFrameTime").text             = $"{model.AverageSample.FullFrameTime:F2}ms";
        viewRoot.Q<Label>("MainThreadCPUFrameTime").text    = $"{model.AverageSample.MainThreadCPUFrameTime:F2}ms";
        viewRoot.Q<Label>("RenderThreadCPUFrameTime").text  = $"{model.AverageSample.RenderThreadCPUFrameTime:F2}ms";
        viewRoot.Q<Label>("GPUFrameTime").text              = $"{model.AverageSample.GPUFrameTime:F2}ms";
    }

    public void ResetUI(VisualElement viewRoot)
    {
        viewRoot.Q<Label>("FullFrameTime").text             = "0.00ms";
        viewRoot.Q<Label>("MainThreadCPUFrameTime").text    = "0.00ms";
        viewRoot.Q<Label>("RenderThreadCPUFrameTime").text  = "0.00ms";
        viewRoot.Q<Label>("GPUFrameTime").text              = "0.00ms";
    }
}
