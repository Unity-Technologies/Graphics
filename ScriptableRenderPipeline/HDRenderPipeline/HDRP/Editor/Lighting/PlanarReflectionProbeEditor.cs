using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.Rendering.HDPipeline;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditorForRenderPipeline(typeof(PlanarReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class PlanarReflectionProbeEditor : Editor
    {
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
            }
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
    }
}
