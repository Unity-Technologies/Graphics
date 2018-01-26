using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(ReflectionProxyVolumeComponent))]
    [CanEditMultipleObjects]
    class ProxyVolumeEditor : Editor
    {
        ReflectionProxyVolumeComponent[] m_TypedTargets;
        SerializedProxyVolumeComponent m_SerializedData;
        ProxyVolumeComponentUI m_UIState = new ProxyVolumeComponentUI();
        ProxyVolumeComponentUI[] m_UIHandlerState;

        void OnEnable()
        {
            m_TypedTargets = targets.Cast<ReflectionProxyVolumeComponent>().ToArray();
            m_SerializedData = new SerializedProxyVolumeComponent(serializedObject);

            m_UIState.Reset(m_SerializedData, Repaint);

            m_UIHandlerState = new ProxyVolumeComponentUI[m_TypedTargets.Length];
            for (var i = 0; i < m_UIHandlerState.Length; i++)
                m_UIHandlerState[i] = new ProxyVolumeComponentUI();
        }

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedData;
            var o = this;

            d.Update();
            s.Update();

            ProxyVolumeComponentUI.Inspector.Draw(s, d, o);

            d.Apply();
        }

        void OnSceneGUI()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
                ProxyVolumeComponentUI.DrawHandles_EditBase(m_UIHandlerState[i], m_TypedTargets[i]);
        }
    }
}
