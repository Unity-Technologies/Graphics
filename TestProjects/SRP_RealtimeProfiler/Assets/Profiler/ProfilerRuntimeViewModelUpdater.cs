using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ProfilerRuntimeViewModelUpdater : MonoBehaviour
{
    UIDocument m_Document;
    ProfilerDataModel m_ProfilerDataModel;
    ProfilerViewModel m_ProfilerViewModel = new ProfilerViewModel();
    
    
    void OnEnable()
    {
        m_Document = GetComponent<UIDocument>();
        m_ProfilerDataModel = FindObjectOfType<ProfilerDataModel>();
    }

    void Update()
    {
        m_ProfilerViewModel.UpdateUI(m_ProfilerDataModel, m_Document.rootVisualElement);
    }
}
