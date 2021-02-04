using System;
using UnityEditor;
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

        // TODO fix
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Packages/com.unity.render-pipelines.core/Resources/RuntimeProfilerPanelSettings.asset");
        if (panelSettings == null)
        {
            // TODO: Create PanelSettings asset inside the package
            Debug.LogError("Failed to initialize Runtime Profiler - UI Toolkit PanelSettings asset not found in project");
            return;
        }

        var go = new GameObject("[Realtime Profiler Updater]");
        DontDestroyOnLoad(go);

        var updater = go.AddComponent<RealtimeProfilerRuntimeUpdater>();
        updater.m_Model = go.AddComponent<RealtimeProfilerModel>();
        updater.m_Document = go.AddComponent<UIDocument>();
        updater.m_Document.panelSettings = panelSettings;
        // TODO: Must use Resources.Load here - AssetDatabase is Editor feature
        updater.m_Document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Resources/UXML/realtimeprofiler-player-container.uxml");
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
