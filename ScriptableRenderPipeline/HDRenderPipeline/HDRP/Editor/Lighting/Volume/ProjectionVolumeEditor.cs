using System.Linq;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(ProjectionVolumeComponent))]
    [CanEditMultipleObjects]
    class ProjectionVolumeEditor : Editor
    {
        ProjectionVolumeComponent[] m_TypedTargets;
        SerializedProjectionVolumeComponent m_SerializedData;
        ProjectionVolumeComponentUI m_UIState = new ProjectionVolumeComponentUI();

        void OnEnable()
        {
            m_TypedTargets = targets.Cast<ProjectionVolumeComponent>().ToArray();
            m_SerializedData = new SerializedProjectionVolumeComponent(serializedObject);

            m_UIState.Reset(m_SerializedData, Repaint);
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
                DoHandles(m_TypedTargets[i]);
        }

        static void DoHandles(ProjectionVolumeComponent target)
        {
            
        }
    }
}
