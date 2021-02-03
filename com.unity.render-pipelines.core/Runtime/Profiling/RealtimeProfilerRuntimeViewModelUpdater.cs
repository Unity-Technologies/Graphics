using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ProfilerRuntimeViewModelUpdater : MonoBehaviour
{
    UIDocument m_Document;
    RealtimeProfilerModel m_RealtimeProfilerModel;
    RealtimeProfilerViewModel m_RealtimeProfilerViewModel = new RealtimeProfilerViewModel();


    void OnEnable()
    {
        m_Document = GetComponent<UIDocument>();
        m_RealtimeProfilerModel = FindObjectOfType<RealtimeProfilerModel>();
    }

    void Update()
    {
        if (m_Document != null)
            m_RealtimeProfilerViewModel.UpdateUI(m_RealtimeProfilerModel, m_Document.rootVisualElement);
    }
}
