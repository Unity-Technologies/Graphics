#if VFX_HAS_TIMELINE
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace UnityEditor.VFX
{
    [CustomEditor(typeof(VisualEffectControlTrack))]
    class VisualEffectControlTrackInspector : Editor
    {
        SerializedProperty reinitProperty;

        private void OnEnable()
        {
            reinitProperty = serializedObject.FindProperty(nameof(VisualEffectControlTrack.reinit));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(reinitProperty);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                //Modification on tracks doesn't trigger a refresh, calling manually the director refresh
                var allDirectors = FindObjectsOfType<UnityEngine.Playables.PlayableDirector>(false);
                foreach (var director in allDirectors)
                {
                    director.RebuildGraph();
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(PlayableTimeSpace))]
    class VisualEffectPlayableSerializedEventTimeSpaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var previousTimeSpace = (PlayableTimeSpace)property.enumValueIndex;
            var newTimeSpace = (PlayableTimeSpace)EditorGUI.EnumPopup(position, label, previousTimeSpace);
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

                var parentPlayable = property.serializedObject.targetObject as VisualEffectControlClip;
                if (parentPlayable == null)
                    throw new InvalidOperationException();

                var oldTime = parentEventTime.doubleValue;
                var newTime = VFXTimeSpaceHelper.GetTimeInSpace(previousTimeSpace, oldTime, newTimeSpace, parentPlayable.clipStart, parentPlayable.clipEnd);
                parentEventTime.doubleValue = newTime;
            }
        }
    }

    [CustomPropertyDrawer(typeof(UnityEngine.VFX.EventAttributes))]
    class EventAttributesDrawer : PropertyDrawer
    {
        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var reordableList = VisualEffectControlClipInspector.GetOrBuildEventAttributeList(property.serializedObject.targetObject as VisualEffectControlClip, property);

            if (reordableList != null)
                return reordableList.GetHeight();

            return 0.0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var reordableList = VisualEffectControlClipInspector.GetOrBuildEventAttributeList(property.serializedObject.targetObject as VisualEffectControlClip, property);
            if (reordableList != null)
            {
                EditorGUI.BeginChangeCheck();
                reordableList.DoList(position);
                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
            }
        }
    }

    [CustomEditor(typeof(VisualEffectControlClip))]
    class VisualEffectControlClipInspector : Editor
    {
        SerializedProperty scrubbingProperty;
        SerializedProperty startSeedProperty;
        SerializedProperty reinitProperty;
        SerializedProperty prewarmEnable;
        SerializedProperty prewarmStepCount;
        SerializedProperty prewarmDeltaTime;
        SerializedProperty prewarmEvent;

        ReorderableList m_ReoderableClipEvents;
        ReorderableList m_ReoderableSingleEvents;

        static private List<(VisualEffectControlClip asset, VisualEffectControlClipInspector inspector)> s_RegisteredInspector = new List<(VisualEffectControlClip asset, VisualEffectControlClipInspector inspector)>();
        Dictionary<string, ReorderableList> m_CacheEventAttributeReordableList = new Dictionary<string, ReorderableList>();

        private static readonly List<(Type type, Type valueType)> kEventAttributeSpecialization = new(GetEventAttributeSpecialization());

        private static IEnumerable<(Type type, Type valueType)> GetEventAttributeSpecialization()
        {
            var subClasses = VFXLibrary.FindConcreteSubclasses(typeof(EventAttribute));
            foreach (var eventAttribute in subClasses)
            {
                var valueType = eventAttribute.GetMember(nameof(EventAttributeValue<char>.value));
                yield return (eventAttribute, ((FieldInfo)valueType[0]).FieldType);
            }
        }

        private static IEnumerable<EventAttribute> GetAvailableAttributes()
        {
            foreach (var attributeName in VFXAttribute.AllIncludingVariadicReadWritable)
            {
                var attribute = VFXAttribute.Find(attributeName);
                var type = VFXExpression.TypeToType(attribute.type);

                EventAttribute eventAttribute = null;

                if (type == typeof(Vector3))
                {
                    if (attribute.name.Contains("color"))
                        eventAttribute = new EventAttributeColor();
                    else
                        eventAttribute = new EventAttributeVector3();
                }
                else
                {
                    var findTypeIndex = kEventAttributeSpecialization.FindIndex(o => o.valueType == type);
                    if (findTypeIndex == -1)
                        throw new InvalidOperationException("Unexpected type : " + type);
                    eventAttribute = (EventAttribute)Activator.CreateInstance(kEventAttributeSpecialization[findTypeIndex].type);
                }

                if (eventAttribute != null)
                {
                    eventAttribute.id = attribute.name;
                    var valueField = eventAttribute.GetType().GetField(nameof(EventAttributeValue<byte>.value));
                    valueField.SetValue(eventAttribute, attribute.value.GetContent());
                    yield return eventAttribute;
                }
            }

            foreach (var custom in kEventAttributeSpecialization)
            {
                var eventAttribute = (EventAttribute)Activator.CreateInstance(custom.type);
                eventAttribute.id = "Custom " + custom.type.Name.Replace("EventAttribute", string.Empty);
                yield return eventAttribute;
            }
        }

        private static readonly List<EventAttribute> kAvailableAttributes = new(GetAvailableAttributes());

        public static ReorderableList GetOrBuildEventAttributeList(VisualEffectControlClip asset, SerializedProperty property)
        {
            var inspectorIndex = s_RegisteredInspector.FindIndex(o => o.asset == asset);
            if (inspectorIndex == -1)
                return null;

            var inspector = s_RegisteredInspector[inspectorIndex].inspector;
            if (inspector == null)
                return null;

            var path = property.propertyPath;
            if (!inspector.m_CacheEventAttributeReordableList.TryGetValue(path, out var reorderableList))
            {
                var emptyGUIContent = new GUIContent(string.Empty);

                var contentProperty = property.FindPropertyRelative(nameof(UnityEngine.VFX.EventAttributes.content));
                reorderableList = new ReorderableList(property.serializedObject, contentProperty, true, true, true, true);
                reorderableList.drawHeaderCallback += (Rect r) => { EditorGUI.LabelField(r, "Attributes"); };
                reorderableList.onAddDropdownCallback += (Rect buttonRect, ReorderableList list) =>
                {
                    var menu = new GenericMenu();
                    foreach (var option in kAvailableAttributes)
                    {
                        menu.AddItem(new GUIContent((string)option.id), false, () =>
                        {
                            contentProperty.serializedObject.Update();
                            contentProperty.arraySize++;
                            var newEntry = contentProperty.GetArrayElementAtIndex(contentProperty.arraySize - 1);
                            var newValue = DeepClone(option);
                            newEntry.managedReferenceValue = newValue;
                            contentProperty.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    menu.ShowAsContext();
                };

                reorderableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
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

                    EditorGUI.PropertyField(new Rect(rect.x, rect.y + 2, labelSize.x - 2, EditorGUIUtility.singleLineHeight), attributeName, emptyGUIContent);
                    var valueRect = new Rect(rect.x + labelSize.x, rect.y + 2, rect.width - labelSize.x, EditorGUIUtility.singleLineHeight);
                    if (attributeProperty.managedReferenceValue is EventAttributeColor)
                    {
                        var oldVector3 = attributeValue.vector3Value;
                        var oldColor = new Color(oldVector3.x, oldVector3.y, oldVector3.z);
                        EditorGUI.BeginChangeCheck();
                        var newColor = EditorGUI.ColorField(valueRect, oldColor);
                        if (EditorGUI.EndChangeCheck())
                            attributeValue.vector3Value = new Vector3(newColor.r, newColor.g, newColor.b);
                    }
                    else if (attributeProperty.managedReferenceValue is EventAttributeVector4)
                    {
                        var oldVector4 = attributeValue.vector4Value;
                        EditorGUI.BeginChangeCheck();
                        var newVector4 = EditorGUI.Vector4Field(valueRect, GUIContent.none, oldVector4);
                        if (EditorGUI.EndChangeCheck())
                            attributeValue.vector4Value = newVector4;
                    }
                    else
                    {
                        EditorGUI.PropertyField(valueRect, attributeValue, emptyGUIContent);
                    }
                };

                inspector.m_CacheEventAttributeReordableList.Add(path, reorderableList);
            }
            return reorderableList;
        }

        private void OnDisable()
        {
            s_RegisteredInspector.RemoveAll(o => o.inspector == this);
        }

        ReorderableList BuildEventReordableList(SerializedObject serializedObject, SerializedProperty property)
        {
            var reordableList = new ReorderableList(serializedObject, property, true, true, true, true);
            reordableList.elementHeightCallback += (int index) =>
            {
                var clipEvent = property.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(clipEvent, true);
            };

            reordableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var clipEvent = property.GetArrayElementAtIndex(index);
                var newRect = rect;
                newRect.x += 8;
                EditorGUI.PropertyField(newRect, clipEvent, true);
            };
            return reordableList;
        }

        private static VisualEffectPlayableSerializedEvent DeepClone(VisualEffectPlayableSerializedEvent source)
        {
            VisualEffectPlayableSerializedEvent newEvent = source;
            newEvent.name = DeepClone(newEvent.name);
            newEvent.eventAttributes.content = DeepClone(newEvent.eventAttributes.content);
            return newEvent;
        }

        private static VisualEffectPlayableSerializedEventNoColor DeepClone(VisualEffectPlayableSerializedEventNoColor source)
        {
            VisualEffectPlayableSerializedEventNoColor newEvent = source;
            newEvent.name = DeepClone(newEvent.name);
            newEvent.eventAttributes.content = DeepClone(newEvent.eventAttributes.content);
            return newEvent;
        }

        private static ExposedProperty DeepClone(ExposedProperty source)
        {
            ExposedProperty newExposedProperty = (string)source;
            if (object.ReferenceEquals(source, newExposedProperty))
                throw new InvalidOperationException();
            return newExposedProperty;
        }

        private static EventAttribute DeepClone(EventAttribute source)
        {
            if (source == null)
                return null;

            var referenceType = source.GetType();
            var copy = (EventAttribute)Activator.CreateInstance(referenceType);
            copy.id = source.id;
            var valueField = referenceType.GetField(nameof(EventAttributeValue<byte>.value));
            valueField.SetValue(copy, valueField.GetValue(source));
            return copy;
        }

        private static EventAttribute[] DeepClone(EventAttribute[] source)
        {
            if (source == null)
                return Array.Empty<EventAttribute>();

            var newEventAttributeArray = new EventAttribute[source.Length];
            for (int i = 0; i < newEventAttributeArray.Length; ++i)
            {
                var reference = source[i];
                newEventAttributeArray[i] = DeepClone(reference);
            }
            return newEventAttributeArray;
        }

        static Color[] kNiceColor = new Color[]
        {
            new Color32(123, 158, 5, 255),
            new Color32(52, 136, 167, 255),
            new Color32(204, 112, 0, 255),
            new Color32(90, 178, 188, 255),
            new Color32(114, 104, 12, 255),
            new Color32(197, 162, 6, 255),
            new Color32(136, 40, 7, 255),
            new Color32(97, 73, 133, 255),
            new Color32(122, 123, 30, 255),
            new Color32(80, 160, 93, 255)
        };

        private static Color SmartPickingNewColor(VisualEffectControlClip controlClip)
        {
            var candidateColor = new List<Color>(kNiceColor);

            foreach (var clipEvent in controlClip.clipEvents)
                candidateColor.Remove(clipEvent.editorColor);
            foreach (var singleEvent in controlClip.singleEvents)
                candidateColor.Remove(singleEvent.editorColor);

            if (candidateColor.Count > 0)
                return candidateColor[0];

            //Arbitrary picking (but not random) of color
            return kNiceColor[(controlClip.clipEvents.Count + controlClip.singleEvents.Count) % kNiceColor.Length];
        }

        private bool AnyDuplicatedReference(EventAttribute[] eventAttributes, HashSet<EventAttribute> alreadyFoundEventAttributes)
        {
            if (eventAttributes != null)
            {
                foreach (var eventAttribute in eventAttributes)
                {
                    if (alreadyFoundEventAttributes.Contains(eventAttribute))
                        return true;
                    alreadyFoundEventAttributes.Add(eventAttribute);
                }
            }

            return false;
        }

        private bool AnyDuplicatedReference(List<VisualEffectControlClip.ClipEvent> clipEvents)
        {
            var alreadyFoundEventAttributes = new HashSet<EventAttribute>();
            foreach (var clipEvent in clipEvents)
            {
                if (AnyDuplicatedReference(clipEvent.enter.eventAttributes.content, alreadyFoundEventAttributes))
                    return true;
                if (AnyDuplicatedReference(clipEvent.exit.eventAttributes.content, alreadyFoundEventAttributes))
                    return true;
            }
            return false;
        }

        private bool AnyDuplicatedReference(List<VisualEffectPlayableSerializedEvent> singleEvents)
        {
            var alreadyFoundEventAttributes = new HashSet<EventAttribute>();
            foreach (var singleEvent in singleEvents)
            {
                if (AnyDuplicatedReference(singleEvent.eventAttributes.content, alreadyFoundEventAttributes))
                    return true;
            }
            return false;
        }

        private void OnEnable()
        {
            s_RegisteredInspector.Add((target as VisualEffectControlClip, this));

            scrubbingProperty = serializedObject.FindProperty(nameof(VisualEffectControlClip.scrubbing));
            startSeedProperty = serializedObject.FindProperty(nameof(VisualEffectControlClip.startSeed));
            reinitProperty = serializedObject.FindProperty(nameof(VisualEffectControlClip.reinit));

            var prewarmSettings = serializedObject.FindProperty(nameof(VisualEffectControlClip.prewarm));
            prewarmEnable = prewarmSettings.FindPropertyRelative(nameof(VisualEffectControlClip.PrewarmClipSettings.enable));
            prewarmStepCount = prewarmSettings.FindPropertyRelative(nameof(VisualEffectControlClip.PrewarmClipSettings.stepCount));
            prewarmDeltaTime = prewarmSettings.FindPropertyRelative(nameof(VisualEffectControlClip.PrewarmClipSettings.deltaTime));
            prewarmEvent = prewarmSettings.FindPropertyRelative(nameof(VisualEffectControlClip.PrewarmClipSettings.eventName) + ".m_Name");

            var clipEventsProperty = serializedObject.FindProperty(nameof(VisualEffectControlClip.clipEvents));
            var singleEventsProperty = serializedObject.FindProperty(nameof(VisualEffectControlClip.singleEvents));

            m_ReoderableClipEvents = BuildEventReordableList(serializedObject, clipEventsProperty);
            m_ReoderableClipEvents.drawHeaderCallback += (Rect r) => { EditorGUI.LabelField(r, "Clip Events"); };
            m_ReoderableClipEvents.onAddCallback = (ReorderableList list) =>
            {
                var playable = (VisualEffectControlClip)clipEventsProperty.serializedObject.targetObject;
                Undo.RegisterCompleteObjectUndo(playable, "Add new clip event");
                clipEventsProperty.serializedObject.ApplyModifiedProperties();

                var newColor = SmartPickingNewColor(playable);

                var newClipEvent = new VisualEffectControlClip.ClipEvent();
                if (playable.clipEvents.Count > 0)
                {
                    var last = playable.clipEvents[^1];
                    newClipEvent.editorColor = newColor;
                    newClipEvent.enter = DeepClone(last.enter);
                    newClipEvent.exit = DeepClone(last.exit);
                }
                else
                {
                    newClipEvent.editorColor = newColor;

                    newClipEvent.enter.eventAttributes = new UnityEngine.VFX.EventAttributes();
                    newClipEvent.enter.name = VisualEffectAsset.PlayEventName;
                    newClipEvent.enter.time = 0.0;
                    newClipEvent.enter.timeSpace = PlayableTimeSpace.AfterClipStart;
                    newClipEvent.enter.eventAttributes.content = Array.Empty<EventAttribute>();

                    newClipEvent.exit.eventAttributes = new UnityEngine.VFX.EventAttributes();
                    newClipEvent.exit.name = VisualEffectAsset.StopEventName;
                    newClipEvent.exit.time = 0.0;
                    newClipEvent.exit.timeSpace = PlayableTimeSpace.BeforeClipEnd;
                    newClipEvent.exit.eventAttributes.content = Array.Empty<EventAttribute>();
                }
                playable.clipEvents.Add(newClipEvent);
                clipEventsProperty.serializedObject.Update();
            };

            m_ReoderableSingleEvents = BuildEventReordableList(serializedObject, singleEventsProperty);
            m_ReoderableSingleEvents.drawHeaderCallback += (Rect r) => { EditorGUI.LabelField(r, "Single Events"); };
            m_ReoderableSingleEvents.onAddCallback = (ReorderableList list) =>
            {
                var playable = (VisualEffectControlClip)singleEventsProperty.serializedObject.targetObject;
                Undo.RegisterCompleteObjectUndo(playable, "Add new single event");
                singleEventsProperty.serializedObject.ApplyModifiedProperties();

                var newSingleEvent = new VisualEffectPlayableSerializedEvent();
                if (playable.singleEvents.Count > 0)
                {
                    var last = playable.singleEvents[^1];
                    newSingleEvent = DeepClone(last);
                }
                newSingleEvent.editorColor = SmartPickingNewColor(playable);

                playable.singleEvents.Add(newSingleEvent);
                singleEventsProperty.serializedObject.Update();
            };

            //Detect duplication of serialized reference (e.g.: 'Duplicate Array Element' is called)
            m_ReoderableClipEvents.onChangedCallback += (ReorderableList list) =>
            {
                var playable = (VisualEffectControlClip)singleEventsProperty.serializedObject.targetObject;
                if (AnyDuplicatedReference(playable.clipEvents))
                {
                    for (int i = 0; i < playable.clipEvents.Count; ++i)
                    {
                        var source = playable.clipEvents[i];
                        var copy = new VisualEffectControlClip.ClipEvent();
                        copy.editorColor = source.editorColor;
                        copy.enter = DeepClone(source.enter);
                        copy.exit = DeepClone(source.exit);
                        playable.clipEvents[i] = copy;
                    }
                }
            };

            m_ReoderableSingleEvents.onChangedCallback += (ReorderableList list) =>
            {
                var playable = (VisualEffectControlClip)singleEventsProperty.serializedObject.targetObject;
                if (AnyDuplicatedReference(playable.singleEvents))
                {
                    for (int i = 0; i < playable.singleEvents.Count; ++i)
                    {
                        var source = playable.singleEvents[i];
                        var copy = DeepClone(source);
                        playable.singleEvents[i] = copy;
                    }
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(scrubbingProperty);

            var currentReinit = (VisualEffectControlClip.ReinitMode)reinitProperty.enumValueIndex;

            if (scrubbingProperty.boolValue)
                currentReinit = VisualEffectControlClip.ReinitMode.OnEnterOrExitClip;

            using (new EditorGUI.DisabledScope(scrubbingProperty.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                var newReinit = (VisualEffectControlClip.ReinitMode)EditorGUILayout.EnumPopup(EditorGUIUtility.TrTextContent("Reinit"), currentReinit);
                if (EditorGUI.EndChangeCheck())
                {
                    reinitProperty.enumValueIndex = (int)newReinit;
                }
            }

            using (new EditorGUI.DisabledScope(!(
                 currentReinit == VisualEffectControlClip.ReinitMode.OnEnterOrExitClip
                || currentReinit == VisualEffectControlClip.ReinitMode.OnEnterClip)))
            {
                EditorGUILayout.PropertyField(startSeedProperty);
                EditorGUILayout.PropertyField(prewarmEnable, EditorGUIUtility.TrTextContent("Enable PreWarm"));
                using (new EditorGUI.DisabledScope(!prewarmEnable.boolValue))
                {
                    VisualEffectAssetEditor.DisplayPrewarmInspectorGUI(serializedObject, prewarmDeltaTime, prewarmStepCount);
                    EditorGUILayout.PropertyField(prewarmEvent, EditorGUIUtility.TrTextContent("PreWarm Event Name"));
                }
            }

            m_ReoderableClipEvents.DoLayoutList();
            m_ReoderableSingleEvents.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
