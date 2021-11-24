using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditorInternal;

namespace UnityEngine.Rendering.HighDefinition
{
    [CustomEditor(typeof(CapsuleOccluder))]
    public class CapsuleOccluderEditor : Editor {
        static EditMode.SceneViewEditMode[] s_EditModes = new EditMode.SceneViewEditMode[]{
            (EditMode.SceneViewEditMode)100,
            (EditMode.SceneViewEditMode)101,
        };
        static GUIContent[] s_EditModesContent;

        SerializedProperty m_Center, m_Radius, m_Height, m_Range;

        private void OnEnable()
        {
            m_Center = serializedObject.FindProperty("center");
            m_Radius = serializedObject.FindProperty("radius");
            m_Height = serializedObject.FindProperty("height");
            m_Range = serializedObject.FindProperty("range");

            if (s_EditModesContent == null)
            {
                s_EditModesContent = new GUIContent[]
                {
                    EditorGUIUtility.TrIconContent("MoveTool", "Translate."),
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

        public void OnSceneGUI()
        {
            var t = target as CapsuleOccluder;

            Handles.matrix = t.capsuleToWorld;
            Handles.color = Color.white;
            Handles.zTest = CompareFunction.LessEqual;
            DrawWireCapsule(t.radius, t.height);
            Handles.color = Color.grey;
            Handles.zTest = CompareFunction.Greater;
            DrawWireCapsule(t.radius, t.height);

            Handles.matrix = t.transform.localToWorldMatrix;
            Handles.zTest = CompareFunction.Always;
            if (EditMode.editMode == s_EditModes[0])
            {
                EditorGUI.BeginChangeCheck();
                Vector3 center = Handles.PositionHandle(t.center, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                    m_Center.vector3Value = center;
            }
            if (EditMode.editMode == s_EditModes[1])
            {
                float handleScale = 0.025f;
                Vector3 position = t.center;
                Handles.color = Color.yellow;

                EditorGUI.BeginChangeCheck();
                position = t.center + new Vector3(t.radius, 0.0f, 0.0f);
                position = Handles.Slider(position, Vector3.right, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                    m_Radius.floatValue = position.x - t.center.x;

                EditorGUI.BeginChangeCheck();
                position = t.center + new Vector3(-t.radius, 0.0f, 0.0f);
                position = Handles.Slider(position, -Vector3.right, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                    m_Radius.floatValue = -(position.x - t.center.x);

                EditorGUI.BeginChangeCheck();
                position = t.center + new Vector3(0.0f, t.radius, 0.0f);
                position = Handles.Slider(position, Vector3.up, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                    m_Radius.floatValue = position.y - t.center.y;

                EditorGUI.BeginChangeCheck();
                position = t.center + new Vector3(0.0f, -t.radius, 0.0f);
                position = Handles.Slider(position, -Vector3.up, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                    m_Radius.floatValue = -(position.y - t.center.y);

                EditorGUI.BeginChangeCheck();
                position = t.center + new Vector3(0.0f, 0.0f, 0.5f * t.height);
                position = Handles.Slider(position, Vector3.forward, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                    m_Height.floatValue = 2.0f * (position.z - t.center.z);

                EditorGUI.BeginChangeCheck();
                position = t.center + new Vector3(0.0f, 0.0f, -0.5f * t.height);
                position = Handles.Slider(position, -Vector3.forward, handleScale * HandleUtility.GetHandleSize(position), Handles.DotHandleCap, 0.5f);
                if (EditorGUI.EndChangeCheck())
                    m_Height.floatValue = -2.0f * (position.z - t.center.z);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
