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
                if (!VolumeComponentType.FromType(property.serializedObject.targetObject.GetType(), out var componentType))
                    return;

                var archetype = VolumeComponentArchetype.FromTypesCached(componentType);
                if (!archetype.GetOrAddDefaultState(out var defaultState))
                    return;

                if (!defaultState.GetDefaultStateOf(componentType, out var defaultVolumeComponent, out var error))
                {
                    Debug.LogException(error);
                    return;
                }

                Undo.RecordObject(property.serializedObject.targetObject, $"Revert property {property.propertyPath} from {property.serializedObject}");
                SerializedObject serializedObject = new SerializedObject(defaultVolumeComponent);
                var serializedProperty = serializedObject.FindProperty(property.propertyPath);
                property.serializedObject.CopyFromSerializedProperty(serializedProperty);
                property.serializedObject.ApplyModifiedProperties();
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
