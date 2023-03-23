#if UNITY_EDITOR

using System;
using UnityEditor;

namespace UnityEngine.Rendering
{
    public partial class VolumeComponent : IApplyRevertPropertyContextMenuItemProvider
    {
        public bool TryGetRevertMethodForFieldName(SerializedProperty property, out Action<SerializedProperty> revertMethod)
        {
            revertMethod = property =>
            {
                var targetObject = property.serializedObject.targetObject;
                var type = targetObject.GetType();
                var defaultVolumeComponent = (VolumeComponent) CreateInstance(type);
                Undo.RecordObject(targetObject, $"Revert property {property.propertyPath} from {property.serializedObject}");
                SerializedObject serializedObject = new SerializedObject(defaultVolumeComponent);
                var serializedProperty = serializedObject.FindProperty(property.propertyPath);
                property.serializedObject.CopyFromSerializedProperty(serializedProperty);
                property.serializedObject.ApplyModifiedProperties();
                VolumeManager.instance.OnVolumeComponentChanged(targetObject as VolumeComponent);
            };

            return true;
        }

        public string GetSourceTerm()
        {
            return "Property";
        }

        public bool TryGetApplyMethodForFieldName(SerializedProperty property, out Action<SerializedProperty> applyMethod)
        {
            applyMethod = null;
            return false;
        }

        public string GetSourceName(Component comp)
        {
            return string.Empty;
        }
    }
}
#endif
