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
        internal const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;

        static Dictionary<ProbeVolume, HierarchicalBox> shapeBoxes = new Dictionary<ProbeVolume, HierarchicalBox>();

        protected void OnEnable()
        {
            m_SerializedProbeVolume = new SerializedProbeVolume(serializedObject);

            shapeBoxes.Clear();
            for (int i = 0; i < targets.Length; ++i)
            {
                var shapeBox = shapeBoxes[targets[i] as ProbeVolume] = new HierarchicalBox(ProbeVolumeUI.Styles.k_GizmoColorBase, ProbeVolumeUI.Styles.k_BaseHandlesColor);
                shapeBox.monoHandle = false;
            }

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ProbeVolumeUI.Inspector.Draw(m_SerializedProbeVolume, this);

            m_SerializedProbeVolume.Apply();
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(ProbeVolume probeVolume, GizmoType gizmoType)
        {
            using (new Handles.DrawingScope(Matrix4x4.TRS(probeVolume.transform.position, probeVolume.transform.rotation, Vector3.one)))
            {
                // Bounding box.
                if (!shapeBoxes.TryGetValue(probeVolume, out HierarchicalBox shapeBox)) { return; }
                shapeBox.center = Vector3.zero;
                shapeBox.size = probeVolume.parameters.size;
                shapeBox.DrawHull(EditMode.editMode == k_EditShape);
            }

        }

        protected void OnSceneGUI()
        {
            ProbeVolume probeVolume = target as ProbeVolume;

            if (!shapeBoxes.TryGetValue(probeVolume, out HierarchicalBox shapeBox)) { return; }

            {
                    //important: if the origin of the handle's space move along the handle,
                    //handles displacement will appears as moving two time faster.
                    using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, probeVolume.transform.rotation, Vector3.one)))
                    {
                        //contained must be initialized in all case
                        shapeBox.center = Quaternion.Inverse(probeVolume.transform.rotation) * probeVolume.transform.position;
                        shapeBox.size = probeVolume.parameters.size;

                        shapeBox.monoHandle = false;
                        EditorGUI.BeginChangeCheck();
                        shapeBox.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(new Object[] { probeVolume, probeVolume.transform }, "Change Probe Volume Bounding Box");

                            probeVolume.parameters.size = shapeBox.size;

                            Vector3 delta = probeVolume.transform.rotation * shapeBox.center - probeVolume.transform.position;
                            probeVolume.transform.position += delta; ;
                        }
                    }
            }

        }
        }
}
