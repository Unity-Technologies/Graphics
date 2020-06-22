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
        static Mesh sphere = null;
        static Material k_Material = null;

        static EditMode.SceneViewEditMode[] k_EditModes = new EditMode.SceneViewEditMode[]{
            (EditMode.SceneViewEditMode)100, (EditMode.SceneViewEditMode)101, (EditMode.SceneViewEditMode)102
        };
        static GUIContent[] k_ModesContent;
        Material material
        {
            get
            {
                if (k_Material == null || k_Material.Equals(null))
                    k_Material = new Material(Shader.Find("Hidden/UnlitTransparentColored"));
                k_Material.color = new Color(143.0f/255.0f, 134.0f/255.0f, 186.0f/255.0f);
                return k_Material;
            }
        }

        SerializedProperty center, direction, scaling;

        void OnEnable()
        {
            center = serializedObject.FindProperty("center");
            direction = serializedObject.FindProperty("direction");
            scaling = serializedObject.FindProperty("scaling");

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
            EditMode.DoInspectorToolbar(k_EditModes, k_ModesContent, GetBoundsGetter(this), this);
            DrawDefaultInspector();
        }

        public void OnSceneGUI()
        {
            Transform tr = (target as MonoBehaviour).transform;
            Quaternion rot = Quaternion.Euler(direction.vector3Value);
            Handles.matrix = Matrix4x4.TRS(tr.position + center.vector3Value, tr.rotation, Vector3.one);

            serializedObject.Update();

            var mode = ArrayUtility.IndexOf(k_EditModes, EditMode.editMode);
            if (EditMode.editMode == k_EditModes[0])
            {

                EditorGUI.BeginChangeCheck();
                Vector3 new_center = Handles.PositionHandle(Vector3.zero, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    center.vector3Value += new_center;
                }
            }
            else if (EditMode.editMode == k_EditModes[1])
            {
                EditorGUI.BeginChangeCheck();
                rot = Handles.RotationHandle(rot, Vector3.zero);
                if (EditorGUI.EndChangeCheck())
                {
                    direction.vector3Value = rot.eulerAngles;
                }
            }
            else if (EditMode.editMode == k_EditModes[2])
            {
                EditorGUI.BeginChangeCheck();
                Vector3 scale = Handles.ScaleHandle(scaling.vector3Value, Vector3.zero, rot, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    scaling.vector3Value = scale;
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (sphere == null)
                sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            material.SetPass(0);

            GL.wireframe = true;
            //Vector3 scale = Quaternion.Euler(direction.vector3Value).normalized * Vector3.forward * scaling.floatValue;
            Graphics.DrawMeshNow(sphere, Matrix4x4.TRS(tr.position + center.vector3Value, tr.rotation * rot, scaling.vector3Value));
            GL.wireframe = false;
        }
    }
}
