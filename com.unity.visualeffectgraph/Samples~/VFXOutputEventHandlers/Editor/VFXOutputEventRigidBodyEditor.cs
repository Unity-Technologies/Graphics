#if VFX_OUTPUTEVENT_PHYSICS
using UnityEngine;
using UnityEngine.VFX.Utility;
namespace UnityEditor.VFX.Utility
{
    [CustomEditor(typeof(VFXOutputEventRigidBody))]
    public class VFXOutputEventRigidBodyEditor : VFXOutputEventHandlerEditor
    {
        VFXOutputEventRigidBody m_RigidbodyEventHandler;

        SerializedProperty rigidBody;
        SerializedProperty attributeSpace;
        SerializedProperty eventType;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_RigidbodyEventHandler = serializedObject.targetObject as VFXOutputEventRigidBody;

            rigidBody = serializedObject.FindProperty("rigidBody");
            attributeSpace = serializedObject.FindProperty("attributeSpace");
            eventType = serializedObject.FindProperty("eventType");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(rigidBody);
            EditorGUILayout.PropertyField(attributeSpace);
            EditorGUILayout.PropertyField(eventType);

            // Help box
            string helpText = string.Empty;
            switch((VFXOutputEventRigidBody.RigidBodyEventType)(eventType.intValue))
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
            HelpBox("Attribute Usage",helpText);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

        }
    }
}
#endif