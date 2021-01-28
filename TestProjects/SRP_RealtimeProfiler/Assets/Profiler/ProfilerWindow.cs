using System;
using System.Collections.Generic;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
namespace UnityEditor.Rendering
{
    public class ProfilerWindow : EditorWindow
    {
        ProfilerDataModel m_ProfilerDataModelUpdater = null;
        ProfilerViewModel m_ProfilerViewModel = new ProfilerViewModel();

        [MenuItem("Profiler prototype/Profiler")]
        public static void ShowDefaultWindow()
        {
            var wnd = GetWindow<ProfilerWindow>();
            wnd.titleContent = new GUIContent("Realtime GPU Profiler");
        }

        public void OnEnable()
        {
            var availableStatHandles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(availableStatHandles);

            var windowTemplate = EditorGUIUtility.Load("Assets/Profiler/profiler-content.uxml") as VisualTreeAsset;
            if (windowTemplate != null)
                windowTemplate.CloneTree(this.rootVisualElement);

            this.minSize = new Vector2(400, 250);
        }

        public void OnInspectorUpdate()
        {
            if (m_ProfilerDataModelUpdater == null)
            {
                m_ProfilerDataModelUpdater = FindObjectOfType<ProfilerDataModel>();
            }

            if (m_ProfilerDataModelUpdater)
            {
                //var model = m_ProfilerDataModelUpdater.GetComponent<ProfilerDataModel>();
                m_ProfilerViewModel.UpdateUI(m_ProfilerDataModelUpdater, this.rootVisualElement);
            }
        }
    }
}
#endif
