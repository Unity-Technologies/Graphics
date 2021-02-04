using System;
using UnityEngine;
using UnityEngine.UIElements;

public class RealtimeProfilerRuntimeUpdater : MonoBehaviour
{
    UIDocument m_Document;
    RealtimeProfilerModel m_Model;
    RealtimeProfilerViewModel m_ViewModel = new RealtimeProfilerViewModel();

    void OnEnable()
    {
        m_Document = GetComponent<UIDocument>();
        m_Model = RealtimeProfilerModel.GetOrCreateRuntimeInstance();
    }

    void OnDisable()
    {
        RealtimeProfilerModel.DestroyInstance();
        m_Model = null;
    }

    void Update()
    {
        if (m_Document != null && m_Model != null)
            m_ViewModel.UpdateUI(m_Model, m_Document.rootVisualElement);
    }
}
