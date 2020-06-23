using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditorInternal;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// </summary>
    [CustomEditor(typeof(EllipsoidOccluder))]
    public class EllipsoidOccluderEditor : Editor
    {
        static Color color = new Color(127.0f/255.0f, 121.0f/255.0f, 156.0f/255.0f, 0.7f);

        static EditMode.SceneViewEditMode[] k_EditModes = new EditMode.SceneViewEditMode[]{
            (EditMode.SceneViewEditMode)100, (EditMode.SceneViewEditMode)101, (EditMode.SceneViewEditMode)102
        };
        static GUIContent[] k_ModesContent;

        SerializedProperty centerOS, radiusOS, directionOS, scalingOS;

        void OnEnable()
        {
            centerOS = serializedObject.FindProperty("centerOS");
            radiusOS = serializedObject.FindProperty("radiusOS");
            directionOS = serializedObject.FindProperty("directionOS");
            scalingOS = serializedObject.FindProperty("scalingOS");

            if (k_ModesContent == null)
                k_ModesContent = new GUIContent[]{
                    EditorGUIUtility.TrIconContent("MoveTool", "Translate."),
                    EditorGUIUtility.TrIconContent("RotateTool", "Rotate."),
                    EditorGUIUtility.TrIconContent("ScaleTool", "Scale.")
                };
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
            EditMode.DoInspectorToolbar(k_EditModes, k_ModesContent, GetBoundsGetter(this), this);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawDefaultInspector();
        }

        public void OnSceneGUI()
        {
            Transform tr = (target as MonoBehaviour).transform;
            Quaternion rot = Quaternion.Euler(directionOS.vector3Value);

            Vector3 scalev = Vector3.one * radiusOS.floatValue;
            scalev.z *= scalingOS.floatValue;

            Handles.color = color;
            Handles.matrix = Matrix4x4.TRS(tr.position + (tr.rotation * centerOS.vector3Value), tr.rotation * rot, scalev);
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, 0.5f);
            Handles.DrawWireDisc(Vector3.zero, Vector3.up, 0.5f);
            Handles.DrawWireDisc(Vector3.zero, Vector3.right, 0.5f);

            Handles.color = Color.white;
            Handles.matrix = Matrix4x4.TRS(tr.position + centerOS.vector3Value, tr.rotation, Vector3.one);
            serializedObject.Update();

            var mode = ArrayUtility.IndexOf(k_EditModes, EditMode.editMode);
            if (EditMode.editMode == k_EditModes[0])
            {

                EditorGUI.BeginChangeCheck();
                Vector3 new_centerOS = Handles.PositionHandle(Vector3.zero, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    centerOS.vector3Value += new_centerOS;
                }
            }
            else if (EditMode.editMode == k_EditModes[1])
            {
                EditorGUI.BeginChangeCheck();
                rot = Handles.RotationHandle(rot, Vector3.zero);
                if (EditorGUI.EndChangeCheck())
                {
                    directionOS.vector3Value = rot.eulerAngles;
                }
            }
            else if (EditMode.editMode == k_EditModes[2])
            {
                EditorGUI.BeginChangeCheck();
                float scale = Handles.ScaleSlider(scalingOS.floatValue, Vector3.zero, rot * Vector3.forward, rot, 0.5f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    scalingOS.floatValue = scale;
                }
            }

            serializedObject.ApplyModifiedProperties();
            Handles.matrix = Matrix4x4.identity;
        }
    }
}
