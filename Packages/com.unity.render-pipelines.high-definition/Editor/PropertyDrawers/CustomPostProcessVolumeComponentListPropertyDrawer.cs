using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(CustomPostProcessVolumeComponentList))]
    class CustomPostProcessVolumeComponentListPropertyDrawer : PropertyDrawer
    {
        List<Type> FetchAvailableCustomPostProcessVolumesTypes(CustomPostProcessInjectionPoint injectionPoint, SerializedProperty list)
        {
            var listTypes = new List<Type>();

            using (HashSetPool<string>.Get(out var tmp))
            {
                for (int i = 0; i < list.arraySize; i++)
                {
                    tmp.Add(list.GetArrayElementAtIndex(i).stringValue);
                }

                foreach (var type in TypeCache.GetTypesDerivedFrom<CustomPostProcessVolumeComponent>())
                {
                    if (type.IsAbstract || tmp.Contains(type.AssemblyQualifiedName))
                        continue;

                    if (type.GetCustomAttribute<HideInInspector>() != null)
                        continue;

                    var comp = ScriptableObject.CreateInstance(type) as CustomPostProcessVolumeComponent;

                    if (comp != null && comp.injectionPoint == injectionPoint)
                        listTypes.Add(type);

                    CoreUtils.Destroy(comp);
                }
            }

            return listTypes;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new Label(property.propertyPath);
        }

        private static Dictionary<string, UnityEditorInternal.ReorderableList> s_ReorderableList = new();

        UnityEditorInternal.ReorderableList GetList(SerializedProperty property, GUIContent label)
        {
            if (s_ReorderableList.TryGetValue(property.propertyPath, out var reorderableList))
            {
                if (reorderableList != null && reorderableList.serializedProperty != null && !reorderableList.serializedProperty.Equals(null))
                {
                    return reorderableList;
                }
            }

            var currentPostProcessTypes = property.FindPropertyRelative("m_CustomPostProcessTypesAsString");

            var injectionPoint = property.FindPropertyRelative("m_InjectionPoint")
                .GetEnumValue<CustomPostProcessInjectionPoint>();

            FieldInfo fieldInfo = typeof(CustomPostProcessInjectionPoint).GetField(injectionPoint.ToString());
            InspectorNameAttribute attribute = fieldInfo.GetCustomAttribute<InspectorNameAttribute>();

            GUIContent header = attribute != null ? new GUIContent(attribute.displayName, label.tooltip) : label;

            reorderableList = new UnityEditorInternal.ReorderableList(
                property.serializedObject,
                currentPostProcessTypes,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true);
            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                var stringType = currentPostProcessTypes.GetArrayElementAtIndex(index).stringValue;
                var elemType = Type.GetType(stringType);
                EditorGUI.LabelField(rect, elemType == null ? $"Invalid type {stringType}" : elemType.Name);
            };
            reorderableList.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, header, EditorStyles.boldLabel);
            reorderableList.onAddCallback = (list) =>
            {
                var menu = new GenericMenu();

                var listTypes = FetchAvailableCustomPostProcessVolumesTypes(injectionPoint, currentPostProcessTypes);
                foreach (var type in listTypes)
                {
                    menu.AddItem(new GUIContent(type.Name, tooltip: type.AssemblyQualifiedName), false, () =>
                    {
                        int lastPos = currentPostProcessTypes.arraySize;
                        currentPostProcessTypes.InsertArrayElementAtIndex(lastPos);
                        var newProperty = currentPostProcessTypes.GetArrayElementAtIndex(lastPos);
                        newProperty.stringValue = type.AssemblyQualifiedName;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                if (menu.GetItemCount() == 0)
                    menu.AddDisabledItem(new GUIContent("No Custom Post Process Available"));

                menu.ShowAsContext();
            };

            reorderableList.onRemoveCallback = (list) =>
            {
                currentPostProcessTypes.DeleteArrayElementAtIndex(list.index);
                property.serializedObject.ApplyModifiedProperties();
            };

            reorderableList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            s_ReorderableList[property.propertyPath] = reorderableList;

            return reorderableList;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.Update();
            try
            {
                GetList(property, label)?.DoList(position);
            }
            catch (NullReferenceException)
            {
                s_ReorderableList[property.propertyPath] = null;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            try
            {
                return GetList(property, label)?.GetHeight() ?? 0; // List height + Spacing before and after
            }
            catch (NullReferenceException)
            {
                s_ReorderableList[property.propertyPath] = null;
                return 0;
            }
        }
    }
}
