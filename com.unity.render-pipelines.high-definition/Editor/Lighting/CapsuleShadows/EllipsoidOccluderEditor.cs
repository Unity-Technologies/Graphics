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
                k_Material.color = new Color(127.0f/255.0f, 121.0f/255.0f, 156.0f/255.0f, 0.7f);
                return k_Material;
            }
        }

        SerializedProperty center, radius, direction, scaling;

        void OnEnable()
        {
            center = serializedObject.FindProperty("center");
            radius = serializedObject.FindProperty("radius");
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
                float scale = Handles.ScaleSlider(scaling.floatValue, Vector3.zero, rot * Vector3.forward, rot, 0.5f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    scaling.floatValue = scale;
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (sphere == null)
                sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            material.SetPass(0);

            GL.wireframe = true;
            Vector3 scalev = Vector3.one * radius.floatValue;
            scalev.z *= scaling.floatValue;
            Graphics.DrawMeshNow(sphere, Matrix4x4.TRS(tr.position + center.vector3Value, tr.rotation * rot, scalev));
            GL.wireframe = false;
        }
    }
}
