using System;
using UnityEngine;
using UnityEngine.UIElements;

public class RealtimeProfilerRuntimeUpdater : MonoBehaviour
{
    UIDocument m_Document;
    RealtimeProfilerModel m_Model;
    RealtimeProfilerViewModel m_ViewModel = new RealtimeProfilerViewModel();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RuntimeInit()
    {
        if (FindObjectOfType<RealtimeProfilerRuntimeUpdater>() != null)
            return;

        var go = new GameObject("[Realtime Profiler Updater]");
        DontDestroyOnLoad(go);

        var updater = go.AddComponent<RealtimeProfilerRuntimeUpdater>();
        updater.m_Model = go.AddComponent<RealtimeProfilerModel>();
        updater.m_Document = go.AddComponent<UIDocument>();
        updater.m_Document.panelSettings = Resources.Load<PanelSettings>("RuntimeProfilerPanelSettings");
        updater.m_Document.visualTreeAsset = Resources.Load<VisualTreeAsset>("UXML/realtimeprofiler-player-container");
    }

    void OnDisable()
    {
        Destroy(gameObject);
    }

    void Update()
    {
        if (m_Document != null && m_Model != null)
            m_ViewModel.UpdateUI(m_Model, m_Document.rootVisualElement);
    }
}
