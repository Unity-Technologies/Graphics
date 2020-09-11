using UnityEngine;
using UnityEngine.VFX.Utility;
namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventRerouteVFXEvent))]
    public class VFXOutputEventRerouteVFXEventEditor : VFXOutputEventHandlerEditor
    {
        VFXOutputEventRerouteVFXEvent m_RerouteVFXEventHandler;

        SerializedProperty targetVisualEffect;
        SerializedProperty eventToReroute;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_RerouteVFXEventHandler = serializedObject.targetObject as VFXOutputEventRerouteVFXEvent;

            targetVisualEffect = serializedObject.FindProperty("targetVisualEffect");
            eventToReroute = serializedObject.FindProperty("eventToReroute");

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(targetVisualEffect);
            EditorGUILayout.PropertyField(eventToReroute);

            // Help box
            HelpBox("Attribute Usage", "Any VFX Attribute set in the source effect, and that is used in the target effect will be routed.");

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}