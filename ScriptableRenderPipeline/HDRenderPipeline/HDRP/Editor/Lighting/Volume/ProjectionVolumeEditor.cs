using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(ProxyVolumeComponent))]
    [CanEditMultipleObjects]
    class ProjectionVolumeEditor : Editor
    {
        ProxyVolumeComponent[] m_TypedTargets;
        SerializedProjectionVolumeComponent m_SerializedData;
        ProjectionVolumeComponentUI m_UIState = new ProjectionVolumeComponentUI();
        ProjectionVolumeComponentUI[] m_UIHandlerState;

        void OnEnable()
        {
            m_TypedTargets = targets.Cast<ProxyVolumeComponent>().ToArray();
            m_SerializedData = new SerializedProjectionVolumeComponent(serializedObject);

            m_UIState.Reset(m_SerializedData, Repaint);

            m_UIHandlerState = new ProjectionVolumeComponentUI[m_TypedTargets.Length];
            for (var i = 0; i < m_UIHandlerState.Length; i++)
                m_UIHandlerState[i] = new ProjectionVolumeComponentUI();
        }

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedData;
            var o = this;

            d.Update();
            s.Update();

            ProjectionVolumeComponentUI.Inspector.Draw(s, d, o);

            d.Apply();
        }

        void OnSceneGUI()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
                ProjectionVolumeComponentUI.DrawHandles(m_TypedTargets[i], m_UIHandlerState[i]);
        }
    }
}
