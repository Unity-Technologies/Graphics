using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    [EditorTool("Light Anchor", typeof(LightAnchor))]
    public class LightAnchorEditorTool : EditorTool
    {
        GUIContent m_IconContent;
        Dictionary<UnityObject, SerializedObject> m_SerializedObjects = new Dictionary<UnityObject, SerializedObject>();
        Dictionary<UnityObject, LightAnchorHandles> m_LightAnchorHandles = new Dictionary<UnityObject, LightAnchorHandles>();

        public override GUIContent toolbarIcon
        {
            get { return m_IconContent; }
        }

        public override bool IsAvailable()
        {
            return true;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            foreach (var target in targets)
            {
                if (target == null)
                    continue;

                DoTargetGUI(target);
            }
        }

        void OnEnable()
        {
            m_IconContent = new GUIContent(Resources.Load<Texture2D>("LightAnchor_Icon"));
        }

        void DoTargetGUI(UnityObject target)
        {
            var lightAnchor = target as LightAnchor;
            var transform = lightAnchor.transform;
            var lightPosition = transform.position;
            var anchorPosition = transform.TransformPoint(Vector3.forward * lightAnchor.distance);
            var handles = GetHandles(target);

            handles.lightPosition = lightPosition;
            handles.anchorPosition = anchorPosition;

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                handles.OnGUI();

                if (change.changed)
                {
                    if (handles.anchorPosition != anchorPosition)
                    {
                        var parent = transform.parent;
                        var so = GetSerializedObject(transform);
                        so.Update();
                        var prop = so.FindProperty("m_LocalPosition");
                        var localPosition = handles.anchorPosition - transform.forward * lightAnchor.distance;
                        if (parent != null)
                            localPosition = parent.InverseTransformPoint(localPosition);
                        prop.vector3Value = localPosition;
                        so.ApplyModifiedProperties();
                    }

                    if (handles.lightPosition != lightPosition)
                    {
                        {
                            var so = GetSerializedObject(lightAnchor);
                            var prop = so.FindProperty("m_Distance");
                            so.Update();
                            prop.floatValue = (handles.lightPosition - handles.anchorPosition).magnitude;
                            so.ApplyModifiedProperties();
                        }

                        {
                            var parent = transform.parent;
                            var so = GetSerializedObject(transform);
                            var prop = so.FindProperty("m_LocalPosition");
                            so.Update();
                            var localPosition = handles.lightPosition;
                            if (parent != null)
                                localPosition = parent.InverseTransformPoint(localPosition);
                            prop.vector3Value = localPosition;
                            so.ApplyModifiedProperties();
                        }
                    }
                }
            }
        }

        LightAnchorHandles GetHandles(UnityObject obj)
        {
            LightAnchorHandles handles;
            if (!m_LightAnchorHandles.TryGetValue(obj, out handles))
            {
                handles = new LightAnchorHandles();
                m_LightAnchorHandles.Add(obj, handles);
            }

            return handles;
        }

        SerializedObject GetSerializedObject(UnityObject obj)
        {
            SerializedObject so;
            if (!m_SerializedObjects.TryGetValue(obj, out so))
            {
                so = new SerializedObject(obj);
                m_SerializedObjects.Add(obj, so);
            }

            return so;
        }
    }
}
