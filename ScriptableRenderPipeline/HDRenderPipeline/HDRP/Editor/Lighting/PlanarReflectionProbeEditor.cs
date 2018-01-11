using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditorForRenderPipeline(typeof(PlanarReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class PlanarReflectionProbeEditor : Editor
    {
        static Dictionary<PlanarReflectionProbe, PlanarReflectionProbeUI> s_StateMap = new Dictionary<PlanarReflectionProbe, PlanarReflectionProbeUI>();

        public static bool TryGetUIStateFor(PlanarReflectionProbe p, out PlanarReflectionProbeUI r)
        {
            return s_StateMap.TryGetValue(p, out r);
        }

        SerializedPlanarReflectionProbe m_SerializedAsset;
        PlanarReflectionProbeUI m_UIState = new PlanarReflectionProbeUI();
        PlanarReflectionProbeUI[] m_UIHandleState;
        PlanarReflectionProbe[] m_TypedTargets;

        void OnEnable()
        {
            m_SerializedAsset = new SerializedPlanarReflectionProbe(serializedObject);
            m_UIState.Reset(m_SerializedAsset, Repaint);

            m_TypedTargets = new PlanarReflectionProbe[targets.Length];
            m_UIHandleState = new PlanarReflectionProbeUI[m_TypedTargets.Length];
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_TypedTargets[i] = (PlanarReflectionProbe)targets[i];
                m_UIHandleState[i] = new PlanarReflectionProbeUI();
                m_UIHandleState[i].Reset(m_SerializedAsset, Repaint);

                s_StateMap[m_TypedTargets[i]] = m_UIHandleState[i];
            }
        }

        void OnDisable()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
                s_StateMap.Remove(m_TypedTargets[i]);
        }

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedAsset;
            var o = this;

            s.Update();
            d.Update();

            PlanarReflectionProbeUI.Inspector.Draw(s, d, o);

            d.Apply();
        }

        void OnSceneGUI()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
                PlanarReflectionProbeUI.DrawHandles(m_UIHandleState[i], m_TypedTargets[i], this);
        }
    }
}
