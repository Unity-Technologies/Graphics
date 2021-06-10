#if VFX_OUTPUTEVENT_PHYSICS
using UnityEngine;
using UnityEngine.VFX.Utility;
namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventRigidBody))]
    class VFXOutputEventRigidBodyEditor : VFXOutputEventHandlerEditor
    {
        SerializedProperty m_RigidBody;
        SerializedProperty m_AttributeSpace;
        SerializedProperty m_EventType;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_RigidBody = serializedObject.FindProperty(nameof(VFXOutputEventRigidBody.rigidBody));
            m_AttributeSpace = serializedObject.FindProperty(nameof(VFXOutputEventRigidBody.attributeSpace));
            m_EventType = serializedObject.FindProperty(nameof(VFXOutputEventRigidBody.eventType));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawOutputEventProperties();

            EditorGUILayout.PropertyField(m_RigidBody);
            EditorGUILayout.PropertyField(m_AttributeSpace);
            EditorGUILayout.PropertyField(m_EventType);

            var helpText = string.Empty;
            switch ((VFXOutputEventRigidBody.RigidBodyEventType)(m_EventType.intValue))
            {
                default:
                case VFXOutputEventRigidBody.RigidBodyEventType.Impulse:
                    helpText = " - velocity : impulse force";
                    break;
                case VFXOutputEventRigidBody.RigidBodyEventType.Explosion:
                    helpText = " - velocity : magnitude as force\n - position : explosion center\n - size : explosion radius";
                    break;
                case VFXOutputEventRigidBody.RigidBodyEventType.VelocityChange:
                    helpText = " - velocity : new velocity for the RigidBody";
                    break;
            }
            HelpBox("Attribute Usage", helpText);

            if (EditorGUI.EndChangeCheck())
            {
                var newRigidBody = m_RigidBody.objectReferenceValue;
                if (newRigidBody != null
                    &&  PrefabUtility.GetPrefabAssetType(newRigidBody) != PrefabAssetType.NotAPrefab
                    &&  PrefabUtility.GetPrefabInstanceStatus(newRigidBody) != PrefabInstanceStatus.Connected)
                    m_RigidBody.objectReferenceValue = null;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
