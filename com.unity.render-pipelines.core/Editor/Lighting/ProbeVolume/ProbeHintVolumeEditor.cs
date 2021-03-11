using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeHintVolume))]
    internal class ProbeHintVolumeEditor : Editor
    {
        internal const EditMode.SceneViewEditMode k_EditShape = EditMode.SceneViewEditMode.ReflectionProbeBox;

        SerializedProperty m_Extent;
        ProbeHintVolume hintVolume => target as ProbeHintVolume;

        static HierarchicalBox s_ShapeBox;

        protected void OnEnable()
        {
            s_ShapeBox = new HierarchicalBox(ProbeVolumeUI.Styles.k_GizmoColorBase, ProbeVolumeUI.Styles.k_BaseHandlesColor);

            m_Extent = serializedObject.FindProperty("m_Extent");
        }

        public override void OnInspectorGUI()
        {
            var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "HDRenderPipelineAsset")
            {
                serializedObject.Update();

                // ProbeVolumeUI.Inspector.Draw(m_SerializedHintVolume, this);

                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Probe Volume is not a supported feature by this SRP.", MessageType.Error, wide: true);
            }
        }

        static Vector3 GetBrickCenterPosition(Vector3 worldPosition)
        {
            float brickSize = ProbeReferenceVolume.instance.MinBrickSize();

            var pos = new Vector3(
                Mathf.FloorToInt(worldPosition.x * (1.0f / brickSize)) * brickSize,
                Mathf.FloorToInt(worldPosition.y * (1.0f / brickSize)) * brickSize,
                Mathf.FloorToInt(worldPosition.z * (1.0f / brickSize)) * brickSize
            );
            pos += Vector3.one * brickSize / 2;
            return pos;
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(ProbeHintVolume hintVolume, GizmoType gizmoType)
        {
            var rotation = ProbeReferenceVolume.instance.GetTransform().rot;
            using (new Handles.DrawingScope(Matrix4x4.TRS(hintVolume.transform.position, rotation, Vector3.one)))
            {
                s_ShapeBox.center = Vector3.zero;
                s_ShapeBox.size = hintVolume.extent;
                s_ShapeBox.DrawHull(EditMode.editMode == k_EditShape);
            }

            using (new Handles.DrawingScope(Color.red, Matrix4x4.identity))
            {
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                float brickSize = ProbeReferenceVolume.instance.MinBrickSize();
                var halfBrickSize = Vector3.one * brickSize / 2;
                var minPos = GetBrickCenterPosition(hintVolume.transform.position - hintVolume.extent / 2.0f);
                var maxPos = GetBrickCenterPosition(hintVolume.transform.position + hintVolume.extent / 2.0f);
                Gizmos.DrawCube((minPos + maxPos) / 2.0f, (maxPos + halfBrickSize) - (minPos - halfBrickSize));
                Handles.DrawWireCube((minPos + maxPos) / 2.0f, (maxPos + halfBrickSize) - (minPos - halfBrickSize));
            }
        }

        protected void OnSceneGUI()
        {
            var rotation = ProbeReferenceVolume.instance.GetTransform().rot;
            //important: if the origin of the handle's space move along the handle,
            //handles displacement will appears as moving two time faster.
            using (new Handles.DrawingScope(Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one)))
            {
                //contained must be initialized in all case
                s_ShapeBox.center = Quaternion.Inverse(rotation) * hintVolume.transform.position;
                s_ShapeBox.size = hintVolume.extent;

                s_ShapeBox.monoHandle = false;
                EditorGUI.BeginChangeCheck();
                s_ShapeBox.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(new Object[] { hintVolume, hintVolume.transform }, "Change Probe Volume Bounding Box");

                    hintVolume.extent = s_ShapeBox.size;

                    Vector3 delta = rotation * s_ShapeBox.center - hintVolume.transform.position;
                    hintVolume.transform.position += delta;;
                }
            }
        }
    }
}
