using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeVolume))]
    internal class ProbeVolumeEditor : Editor
    {
            SerializedProbeVolume m_SerializedProbeVolume;

        protected void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(ProbeVolume probeVolume, GizmoType gizmoType)
        {
        }

        protected void OnSceneGUI()
        {
        }
    }
}
