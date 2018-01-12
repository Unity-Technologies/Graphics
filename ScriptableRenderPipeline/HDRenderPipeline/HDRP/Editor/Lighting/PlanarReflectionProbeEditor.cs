using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

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

        List<Texture> m_PreviewedTextures = new List<Texture>();

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

        public override bool HasPreviewGUI()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                if (m_TypedTargets[i].texture != null)
                    return true;
            }
            return false;
        }

        public override GUIContent GetPreviewTitle()
        {
            return  _.GetContent("Planar Reflection");
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            m_PreviewedTextures.Clear();
            for (var i = 0; i < m_TypedTargets.Length; i++)
                m_PreviewedTextures.Add(m_TypedTargets[i].texture);

            var space = Vector2.one;
            var rowSize = Mathf.CeilToInt(Mathf.Sqrt(m_PreviewedTextures.Count));
            var size = r.size / rowSize - space * (rowSize - 1);

            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                var row = i / rowSize;
                var col = i % rowSize;
                var itemRect = new Rect(
                    r.x + size.x * row + ((row > 0) ? (row - 1) * space.x : 0),
                    r.y + size.y * col + ((col > 0) ? (col - 1) * space.y : 0),
                    size.x,
                    size.y);
                if (m_PreviewedTextures[i] != null)
                    GUI.DrawTexture(itemRect, m_PreviewedTextures[i]);
                else
                    GUI.Label(itemRect, _.GetContent("Not Available"));
            }
        }
    }
}
