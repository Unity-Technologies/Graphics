using UnityEditor;
using UnityEngine;

namespace UnityEngine.Rendering
{
    [CustomEditor(typeof(CapsuleOccluder))]
    public class CapsuleOccluderEditor : Editor
    {
        private CapsuleOccluder targetScript;
        private Tool previousTool;

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(targetScript.enabled == false);
            if (CapsuleOccluderManager.instance.IsOccluderIgnored(targetScript))
            {
                EditorGUILayout.HelpBox(
                    "There are too many CapsuleOccluders in the Scene, this Capsule is being ignored",
                    MessageType.Warning);
            }
            base.OnInspectorGUI();
            EditorGUI.EndDisabledGroup();
        }

        private void OnEnable()
        {
            targetScript = target as CapsuleOccluder;
            previousTool = Tools.current;
            Tools.current = Tool.None;
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            Tools.current = previousTool;
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            SceneView.duringSceneGui += OnDuringSceneGui;
        }

        private void UnsubscribeFromEvents()
        {
            SceneView.duringSceneGui -= OnDuringSceneGui;
        }

        private void OnDuringSceneGui(SceneView view)
        {
            if (targetScript == null)
            {
                return;
            }

            Handles.matrix = targetScript.transform.localToWorldMatrix;
            Handles.zTest = CompareFunction.Always;

            if (targetScript.enabled == false)
            {
                return;
            }

            Undo.RecordObject(targetScript, "Capsule Transform Changed");
            Vector3 scale = new Vector3(targetScript.m_Radius, targetScript.m_Radius, targetScript.m_Height);
            Handles.TransformHandle(ref targetScript.m_Center, ref targetScript.m_Rotation, ref scale);
            targetScript.m_Radius = Mathf.Abs(targetScript.m_Radius - scale.x) > Mathf.Abs(targetScript.m_Radius - scale.y) ? scale.x: scale.y;
            targetScript.m_Radius = Mathf.Max(targetScript.m_Radius, 0);
            targetScript.m_Height = Mathf.Max(scale.z, 0);
        }

        private void OnSceneGUI()
        {
            if (targetScript == null || targetScript.enabled == false)
            {
                return;
            }

            Event e = Event.current;
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }
            else if (e.type == EventType.Repaint)
            {
                CapsuleShadowsUtils.DrawWireCapsule(targetScript, CapsuleOccluderManager.instance.IsOccluderIgnored(targetScript) ? Color.gray : Color.red);
            }
        }
    }
}
