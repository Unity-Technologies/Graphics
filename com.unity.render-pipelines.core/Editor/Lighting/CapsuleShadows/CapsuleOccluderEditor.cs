using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditorInternal;

namespace UnityEngine.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CapsuleOccluder))]
    public class CapsuleOccluderEditor : Editor {
        static EditMode.SceneViewEditMode[] s_EditModes = new EditMode.SceneViewEditMode[]{
            (EditMode.SceneViewEditMode)100,
            (EditMode.SceneViewEditMode)101,
            (EditMode.SceneViewEditMode)102,
        };
        static GUIContent[] s_EditModesContent;

        SerializedProperty m_Center, m_Rotation, m_Radius, m_Height;

        private void OnEnable()
        {
            m_Center = serializedObject.FindProperty("center");
            m_Rotation = serializedObject.FindProperty("rotation");
            m_Radius = serializedObject.FindProperty("radius");
            m_Height = serializedObject.FindProperty("height");

            if (s_EditModesContent == null)
            {
                s_EditModesContent = new GUIContent[]
                {
                    EditorGUIUtility.TrIconContent("MoveTool", "Translate."),
                    EditorGUIUtility.TrIconContent("RotateTool", "Rotate."),
                    EditorGUIUtility.TrIconContent("ScaleTool", "Scale."),
                };
            }
        }

        internal static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
            {
                var bounds = new Bounds();
                var rp = ((Component)o.target).transform;
                var b = rp.position;
                bounds.Encapsulate(b);
                return bounds;
            };
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditMode.DoInspectorToolbar(s_EditModes, s_EditModesContent, GetBoundsGetter(this), this);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawDefaultInspector();
        }

        internal static void DrawWireCapsule(float radius, float height)
        {
            var offset = Mathf.Max(0.0f, 0.5f * height - radius);
            var capCenter = new Vector3(0.0f, 0.0f, offset);

            Handles.DrawWireDisc(capCenter, Vector3.forward, radius);
            Handles.DrawWireDisc(-capCenter, Vector3.forward, radius);
            Handles.DrawLine(new Vector3(-radius, 0, -offset), new Vector3(-radius, 0, offset));
            Handles.DrawLine(new Vector3( radius, 0, -offset), new Vector3( radius, 0, offset));
            Handles.DrawLine(new Vector3(0, -radius, -offset), new Vector3(0, -radius, offset));
            Handles.DrawLine(new Vector3(0,  radius, -offset), new Vector3(0,  radius, offset));
            Handles.DrawWireArc(capCenter, Vector3.right, Vector3.up, 180, radius);
            Handles.DrawWireArc(-capCenter, Vector3.right, Vector3.up, -180, radius);
            Handles.DrawWireArc(capCenter, Vector3.up, Vector3.right, -180, radius);
            Handles.DrawWireArc(-capCenter, Vector3.up, Vector3.right, 180, radius);
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmo(CapsuleOccluder capsule, GizmoType gizmoType)
        {
            // add an invisible ellipsoid Gizmo just to make this component pickable
            Gizmos.matrix = capsule.capsuleToWorld * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, Mathf.Max(1.0f, 0.5f*capsule.height/capsule.radius)));
            Gizmos.color = Color.clear;
            Gizmos.DrawSphere(Vector3.zero, capsule.radius);

            var col = ((gizmoType & GizmoType.Selected) != 0) ? Color.white : Color.green;
            using (new Handles.DrawingScope(capsule.capsuleToWorld))
            {
                Handles.color = col;
                Handles.zTest = CompareFunction.LessEqual;
                DrawWireCapsule(capsule.radius, capsule.height);
                Handles.color = col * 0.5f;
                Handles.zTest = CompareFunction.Greater;
                DrawWireCapsule(capsule.radius, capsule.height);
            }
        }

        public void OnSceneGUI()
        {
            var occluder = target as CapsuleOccluder;

            Handles.matrix = occluder.transform.localToWorldMatrix;
            Handles.zTest = CompareFunction.Always;
            if (EditMode.editMode == s_EditModes[0])
            {
                EditorGUI.BeginChangeCheck();
                Vector3 center = Handles.PositionHandle(occluder.center, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(occluder, "Undo position change.");
                    occluder.center = center;
                }
            }
            if (EditMode.editMode == s_EditModes[1])
            {
                EditorGUI.BeginChangeCheck();
                Quaternion rotation = Handles.RotationHandle(occluder.rotation, occluder.center);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(occluder, "Undo rotation change.");
                    occluder.rotation = rotation;
                }
            }
            if (EditMode.editMode == s_EditModes[2])
            {
                float handleScale = 0.025f;
                Vector3 position = Vector3.zero;

                Handles.matrix = occluder.capsuleToWorld;
                Handles.color = Color.yellow;

                EditorGUI.BeginChangeCheck();
                position = new Vector3(occluder.radius, 0.0f, 0.0f);
                position = Handles.Slider(position, Vector3.right, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(occluder, "Undo radius change.");
                    occluder.radius = position.x;
                }

                EditorGUI.BeginChangeCheck();
                position = new Vector3(-occluder.radius, 0.0f, 0.0f);
                position = Handles.Slider(position, -Vector3.right, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(occluder, "Undo radius change.");
                    occluder.radius = -position.x;
                }

                EditorGUI.BeginChangeCheck();
                position = new Vector3(0.0f, occluder.radius, 0.0f);
                position = Handles.Slider(position, Vector3.up, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(occluder, "Undo radius change.");
                    occluder.radius = position.y;
                }

                EditorGUI.BeginChangeCheck();
                position = new Vector3(0.0f, -occluder.radius, 0.0f);
                position = Handles.Slider(position, -Vector3.up, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(occluder, "Undo radius change.");
                    occluder.radius = -position.y;
                }

                EditorGUI.BeginChangeCheck();
                position = new Vector3(0.0f, 0.0f, 0.5f * occluder.height);
                position = Handles.Slider(position, Vector3.forward, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(occluder, "Undo height change.");
                    occluder.height = 2.0f * position.z;
                }

                EditorGUI.BeginChangeCheck();
                position = new Vector3(0.0f, 0.0f, -0.5f * occluder.height);
                position = Handles.Slider(position, -Vector3.forward, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(occluder, "Undo height change.");
                    occluder.height = -2.0f * position.z;
                }
            }
        }
    }
}
