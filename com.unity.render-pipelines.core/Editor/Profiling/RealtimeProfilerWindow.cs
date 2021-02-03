using System;
using System.Collections.Generic;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
namespace UnityEditor.Rendering
{
    public class RealtimeProfilerWindow : EditorWindow
    {
        RealtimeProfilerModel m_RealtimeProfilerModelUpdater;
        RealtimeProfilerViewModel m_RealtimeProfilerViewModel = new RealtimeProfilerViewModel();

        [MenuItem("Window/Analysis/Realtime Profiler")]
        public static void ShowDefaultWindow()
        {
            var wnd = GetWindow<RealtimeProfilerWindow>();
            wnd.titleContent = new GUIContent("Realtime Profiler");
        }

        public void OnEnable()
        {
            var availableStatHandles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(availableStatHandles);

            var windowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Resources/UXML/realtimeprofiler-content.uxml");
            if (windowTemplate != null)
                windowTemplate.CloneTree(this.rootVisualElement);
            this.minSize = new Vector2(400, 250);
        }

        public void OnInspectorUpdate()
        {
            if (m_RealtimeProfilerModelUpdater == null)
            {
                m_RealtimeProfilerModelUpdater = FindObjectOfType<RealtimeProfilerModel>();
            }

            if (m_RealtimeProfilerModelUpdater)
            {
                //var model = m_ProfilerDataModelUpdater.GetComponent<ProfilerDataModel>();
                m_RealtimeProfilerViewModel.UpdateUI(m_RealtimeProfilerModelUpdater, this.rootVisualElement);
            }
        }
    }
}
#endif
