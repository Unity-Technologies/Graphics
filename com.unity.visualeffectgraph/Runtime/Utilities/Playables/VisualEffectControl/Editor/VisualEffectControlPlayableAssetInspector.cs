#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [CustomPropertyDrawer(typeof(VisualEffectPlayableSerializedEvent.TimeSpace))]
    class VisualEffectPlayableSerializedEventTimeSpaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var previousTimeSpace = (VisualEffectPlayableSerializedEvent.TimeSpace)property.enumValueIndex;
            var newTimeSpace = (VisualEffectPlayableSerializedEvent.TimeSpace)EditorGUI.EnumPopup(position, label, previousTimeSpace);
            if (previousTimeSpace != newTimeSpace)
            {
                property.enumValueIndex = (int)newTimeSpace;
                var parentEventPath = property.propertyPath.Substring(0, property.propertyPath.Length - nameof(VisualEffectPlayableSerializedEvent.timeSpace).Length - 1);
                var parentEvent = property.serializedObject.FindProperty(parentEventPath);
                if (parentEvent == null)
                    throw new InvalidOperationException();

                var parentEventTime = parentEvent.FindPropertyRelative(nameof(VisualEffectPlayableSerializedEvent.time));
                if (parentEventTime == null)
                    throw new InvalidOperationException();

                var parentPlayable = property.serializedObject.targetObject as VisualEffectControlPlayableAsset;
                if (parentPlayable == null)
                    throw new InvalidOperationException();

                var oldTime = parentEventTime.doubleValue;
                var newTime = VisualEffectPlayableSerializedEvent.GetTimeInSpace(previousTimeSpace, oldTime, newTimeSpace, parentPlayable.clipStart, parentPlayable.clipEnd);
                parentEventTime.doubleValue = newTime;
            }
        }
    }

    [CustomPropertyDrawer(typeof(EventAttributes))]
    class EventAttributesDrawer : PropertyDrawer
    {
        private ReorderableList m_ReordableList;

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            BuildReordableList(property);
            return m_ReordableList.GetHeight();
        }

        private static readonly (Type type, Type valueType)[] kEventAttributeSpecialization = GetEventAttributeSpecialization().ToArray();

        private static IEnumerable<(Type type, Type valueType)> GetEventAttributeSpecialization()
        {
            var subClasses = VFXLibrary.FindConcreteSubclasses(typeof(EventAttribute));
            foreach (var eventAttribute in subClasses)
            {
                var valueType = eventAttribute.GetMember(nameof(EventAttributeValue<byte>.value));
                yield return (eventAttribute, valueType[0].DeclaringType);
            }
        }

        private static IEnumerable<(string name, Type type)> GetAvailableAttribute()
        {
            foreach (var attributeName in VFXAttribute.AllIncludingVariadicReadWritable)
            {
                var attribute = VFXAttribute.Find(attributeName);
                var type = VFXExpression.TypeToType(attribute.type);
                if (type == typeof(Vector3))
                {
                    if (attribute.name.Contains("color"))
                        yield return (attribute.name, typeof(EventAttributeColor));
                    else
                        yield return (attribute.name, typeof(EventAttributeVector3));
                }
                //TODOPAUL: Idk why it fails (probably wrong type comparison)
                else if (type == typeof(float))
                {
                    yield return (attribute.name, typeof(EventAttributeFloat));
                }
                else if (type == typeof(uint))
                {
                    yield return (attribute.name, typeof(EventAttributeUInt));
                }
                else if (type == typeof(int))
                {
                    yield return (attribute.name, typeof(EventAttributeInt));
                }
                else if (type == typeof(bool))
                {
                    yield return (attribute.name, typeof(EventAttributeBool));
                }
                else
                {
                    var findType = kEventAttributeSpecialization.FirstOrDefault(o => o.valueType == type);
                    if (findType.type == null)
                        throw new InvalidOperationException("Unexpected type : " + type);

                    yield return (attribute.name, findType.type);
                }
            }

            foreach (var custom in kEventAttributeSpecialization)
            {
                yield return ("Custom " + custom.type.Name.Replace("EventAttribute", string.Empty), custom.type);
            }
        }

        void BuildReordableList(SerializedProperty property)
        {
            if (m_ReordableList == null)
            {
                var emptyGUIContent = new GUIContent(string.Empty);

                var contentProperty = property.FindPropertyRelative(nameof(EventAttributes.content));
                m_ReordableList = new ReorderableList(property.serializedObject, contentProperty, true, true, true, true);
                m_ReordableList.drawHeaderCallback += (Rect r) => { EditorGUI.LabelField(r, "Attributes"); };
                m_ReordableList.onAddDropdownCallback += (Rect buttonRect, ReorderableList list) =>
                {
                    var menu = new GenericMenu();
                    foreach (var option in GetAvailableAttribute())
                    {
                        menu.AddItem(new GUIContent(option.name), false, () =>
                        {
                            contentProperty.serializedObject.Update();
                            contentProperty.arraySize++;
                            var newEntry = contentProperty.GetArrayElementAtIndex(contentProperty.arraySize - 1);
                            newEntry.managedReferenceValue = Activator.CreateInstance(option.type);
                            var nameProperty = newEntry.FindPropertyRelative(nameof(EventAttribute.id) + ".m_Name");
                            nameProperty.stringValue = option.name;
                            contentProperty.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    menu.ShowAsContext();
                };

                m_ReordableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty attributeProperty = contentProperty.GetArrayElementAtIndex(index);
                    SerializedProperty attributeName = null, attributeValue = null;

                    if (attributeProperty != null)
                    {
                        attributeName = attributeProperty.FindPropertyRelative(nameof(EventAttribute.id) + ".m_Name");
                        attributeValue = attributeProperty.FindPropertyRelative(nameof(EventAttributeValue<byte>.value));
                    }

                    if (attributeName == null || attributeValue == null)
                    {
                        EditorGUI.LabelField(rect, "NULL");
                        return;
                    }

                    var labelSize = GUI.skin.label.CalcSize(new GUIContent(attributeName.stringValue));
                    if (labelSize.x < 110)
                        labelSize.x = 110;

                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, labelSize.x - 2, rect.height), attributeName, emptyGUIContent);
                    var valueRect = new Rect(rect.x + labelSize.x, rect.y, rect.width - labelSize.x, rect.height);
                    if (attributeProperty.managedReferenceValue is EventAttributeColor)
                    {
                        var oldVector3 = attributeValue.vector3Value;
                        var oldColor = new Color(oldVector3.x, oldVector3.y, oldVector3.z);
                        EditorGUI.BeginChangeCheck();
                        var newColor = EditorGUI.ColorField(valueRect, oldColor);
                        if (EditorGUI.EndChangeCheck())
                            attributeValue.vector3Value = new Vector3(newColor.r, newColor.g, newColor.b);
                    }
                    else
                    {
                        EditorGUI.PropertyField(valueRect, attributeValue, emptyGUIContent);
                    }
                };
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BuildReordableList(property);

            EditorGUI.BeginChangeCheck();
            m_ReordableList.DoList(position);
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(VisualEffectControlPlayableAsset))]
    class VisualEffectControlPlayableAssetInspector : Editor
    {
        private SerializedProperty scrubbingProperty;
        private SerializedProperty startSeedProperty;
        private SerializedProperty clipEventsProperty;
        private SerializedProperty singleEventsProperty;
        private void OnEnable()
        {
            scrubbingProperty = serializedObject.FindProperty(nameof(VisualEffectControlPlayableAsset.scrubbing));
            startSeedProperty = serializedObject.FindProperty(nameof(VisualEffectControlPlayableAsset.startSeed));
            clipEventsProperty = serializedObject.FindProperty(nameof(VisualEffectControlPlayableAsset.clipEvents));
            singleEventsProperty = serializedObject.FindProperty(nameof(VisualEffectControlPlayableAsset.singleEvents));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(scrubbingProperty);
            if (scrubbingProperty.boolValue)
                EditorGUILayout.PropertyField(startSeedProperty);
            EditorGUILayout.PropertyField(clipEventsProperty);
            EditorGUILayout.PropertyField(singleEventsProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
