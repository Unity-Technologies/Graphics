using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    [EditorTool("HD Lens Flare", typeof(UnityEngine.Rendering.HighDefinition.HDLensFlare))]
    public class HDLensFlareEditorTool : EditorTool
    {
        GUIContent m_IconContent;
        Dictionary<UnityObject, SerializedObject> m_SerializedObjects = new Dictionary<UnityObject, SerializedObject>();
        Dictionary<UnityObject, HDLensFlareEditorHandle> m_LightAnchorHandles = new Dictionary<UnityObject, HDLensFlareEditorHandle>();

        /// <summary>
        /// Checks whether the custom editor tool is available based on the state of the editor.
        /// </summary>
        /// <returns>Always return true</returns>
        public override bool IsAvailable()
        {
            return true;
        }

        /// <summary>
        /// Use this method to implement a custom editor tool.
        /// </summary>
        /// <param name="window">The window that is displaying the custom editor tool.</param>
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
            //m_IconContent = new GUIContent(UnityEditor.Rendering.CoreEditorUtils.LoadIcon(@"Packages/com.unity.render-pipelines.core/Editor/Lighting/Icons/", "LightAnchor_Icon", ".png", false));
        }

        void DoTargetGUI(UnityObject target)
        {
            //var lightAnchor = target as LightAnchor;
            //Transform transform = lightAnchor.transform;;
            //Vector3 lightPosition = transform.position;;
            //Vector3 anchorPosition = transform.TransformPoint(Vector3.forward * lightAnchor.distance);

            //var handles = GetHandles(target);

            //handles.lightPosition = lightPosition;
            //handles.anchorPosition = anchorPosition;

            //using (var change = new EditorGUI.ChangeCheckScope())
            //{
            //    handles.OnGUI();

            //    if (change.changed)
            //    {
            //        if (handles.anchorPosition != anchorPosition)
            //        {
            //            var parent = transform.parent;
            //            var so = GetSerializedObject(transform);
            //            so.Update();
            //            var prop = so.FindProperty("m_LocalPosition");
            //            var localPosition = handles.anchorPosition - transform.forward * lightAnchor.distance;
            //            if (parent != null)
            //                localPosition = parent.InverseTransformPoint(localPosition);
            //            prop.vector3Value = localPosition;
            //            so.ApplyModifiedProperties();
            //        }

            //        if (handles.lightPosition != lightPosition)
            //        {
            //            {
            //                var so = GetSerializedObject(lightAnchor);
            //                var prop = so.FindProperty("m_Distance");
            //                so.Update();
            //                prop.floatValue = (handles.lightPosition - handles.anchorPosition).magnitude;
            //                so.ApplyModifiedProperties();
            //            }

            //            {
            //                var parent = transform.parent;
            //                var so = GetSerializedObject(transform);
            //                var prop = so.FindProperty("m_LocalPosition");
            //                so.Update();
            //                var localPosition = handles.lightPosition;
            //                if (parent != null)
            //                    localPosition = parent.InverseTransformPoint(localPosition);
            //                prop.vector3Value = localPosition;
            //                so.ApplyModifiedProperties();
            //            }
            //        }
            //    }
            //}
        }

        HDLensFlareEditorHandle GetHandles(UnityObject obj)
        {
            HDLensFlareEditorHandle handles;
            if (!m_LightAnchorHandles.TryGetValue(obj, out handles))
            {
                handles = new HDLensFlareEditorHandle();
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
