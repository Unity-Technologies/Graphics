#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [CustomEditor(typeof(VisualEffectControlTrack))]
    class VisualEffectControlTrackInspector : Editor //TODOPAUL: Remove this
    {
        bool showDebugInformation;

        GUIStyle GetGUIStyleFromState(VisualEffectControlTrackMixerBehaviour.ScrubbingCacheHelper.Debug.State debug)
        {
            GUIStyle style = new GUIStyle(EditorStyles.textField);

            switch (debug)
            {
                case VisualEffectControlTrackMixerBehaviour.ScrubbingCacheHelper.Debug.State.OutChunk:
                    style.normal.textColor = Color.white;
                    break;
                case VisualEffectControlTrackMixerBehaviour.ScrubbingCacheHelper.Debug.State.Playing:
                    style.normal.textColor = Color.green;
                    break;
                case VisualEffectControlTrackMixerBehaviour.ScrubbingCacheHelper.Debug.State.ScrubbingBackward:
                    style.normal.textColor = Color.red;
                    break;
                case VisualEffectControlTrackMixerBehaviour.ScrubbingCacheHelper.Debug.State.ScrubbingForward:
                    style.normal.textColor = new Color(1.0f, 0.5f, 0.0f);
                    break;
            }

            return style;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            VisualEffectControlTrackMixerBehaviour.ScrubbingCacheHelper.s_MaximumScrubbingTime
                = EditorGUILayout.FloatField("Maximum Scrubbing Time", VisualEffectControlTrackMixerBehaviour.ScrubbingCacheHelper.s_MaximumScrubbingTime);

            var track = target as VisualEffectControlTrack;
            if (track == null)
                return;

            var mixer = track.lastCreatedMixer;
            if (mixer == null)
                return;

            var debugFrames = mixer.GetDebugInfo();
            if (debugFrames == null)
                return;

            showDebugInformation = EditorGUILayout.Foldout(showDebugInformation, "Debug Infos");
            if (showDebugInformation)
            {
                var stringBuilder = new System.Text.StringBuilder();
                foreach (var debug in debugFrames.Reverse())
                {
                    EditorGUILayout.LabelField(debug.state.ToString(), GetGUIStyleFromState(debug.state));
                    stringBuilder.Clear();
                    stringBuilder.AppendFormat("Chunk: {0}", debug.lastChunk);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("Event: {0}", debug.lastEvent);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("Playable Time: {0}", debug.lastPlayableTime);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("VFX Time: {0}", debug.vfxTime);
                    stringBuilder.AppendLine();
                    stringBuilder.AppendFormat("Delta Time: {0}", debug.vfxTime);
                    if (debug.clipState != null)
                    {
                        var clipStateString = debug.clipState.Select(o => o.ToString()).Aggregate((a, b) => a + ", " + b);
                        stringBuilder.AppendFormat("Clip State: {0}", clipStateString);
                    }

                    EditorGUILayout.TextArea(stringBuilder.ToString());
                }
            }
        }
    }

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
            var reordableList = VisualEffectControlPlayableAssetInspector.GetOrBuildEventAttributeList(property.serializedObject.targetObject as VisualEffectControlPlayableAsset, property);

            if (reordableList != null)
                return reordableList.GetHeight();

            return 0.0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var reordableList = VisualEffectControlPlayableAssetInspector.GetOrBuildEventAttributeList(property.serializedObject.targetObject as VisualEffectControlPlayableAsset, property);
            if (reordableList != null)
            {
                EditorGUI.BeginChangeCheck();
                reordableList.DoList(position);
                if (EditorGUI.EndChangeCheck())
                    property.serializedObject.ApplyModifiedProperties();
            }
        }
    }

    [CustomEditor(typeof(VisualEffectControlPlayableAsset))]
    class VisualEffectControlPlayableAssetInspector : Editor
    {
        SerializedProperty scrubbingProperty;
        SerializedProperty startSeedProperty;

        ReorderableList m_ReoderableClipEvents;
        ReorderableList m_ReoderableSingleEvents;

        static private List<(VisualEffectControlPlayableAsset asset, VisualEffectControlPlayableAssetInspector inspector)> s_RegisteredInspector = new List<(VisualEffectControlPlayableAsset asset, VisualEffectControlPlayableAssetInspector inspector)>();
        Dictionary<string, ReorderableList> m_CacheEventAttributes = new Dictionary<string, ReorderableList>();

        private static readonly (Type type, Type valueType)[] kEventAttributeSpecialization = GetEventAttributeSpecialization().ToArray();

        private static IEnumerable<(Type type, Type valueType)> GetEventAttributeSpecialization()
        {
            var subClasses = VFXLibrary.FindConcreteSubclasses(typeof(EventAttribute));
            foreach (var eventAttribute in subClasses)
            {
                var valueType = eventAttribute.GetMember(nameof(EventAttributeValue<char>.value));
                yield return (eventAttribute, ((FieldInfo)valueType[0]).FieldType);
            }
        }

        private static IEnumerable<(string name, Type type)> GetAvailableAttributes()
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

        private static readonly (string name, Type type)[] kAvailableAttributes = GetAvailableAttributes().ToArray();

        public static ReorderableList GetOrBuildEventAttributeList(VisualEffectControlPlayableAsset asset, SerializedProperty property)
        {
            var inspector = s_RegisteredInspector.FirstOrDefault(o => o.asset == asset).inspector;
            if (inspector == null)
                return null;

            var path = property.propertyPath;
            if (!inspector.m_CacheEventAttributes.TryGetValue(path, out var reorderableList))
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

                inspector.m_CacheEventAttributes.Add(path, reorderableList);
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

        private static EventAttribute[] DeepClone(EventAttribute[] source)
        {
            if (source == null)
                return new EventAttribute[0];

            var newEventAttributeArray = new EventAttribute[source.Length];
            for (int i = 0; i < newEventAttributeArray.Length; ++i)
            {
                var reference = source[i];
                if (reference != null)
                {
                    var referenceType = reference.GetType();
                    var copy = (EventAttribute)Activator.CreateInstance(referenceType);
                    copy.id = reference.id;
                    var valueField = referenceType.GetField(nameof(EventAttributeValue<byte>.value));
                    valueField.SetValue(copy, valueField.GetValue(reference));
                    newEventAttributeArray[i] = copy;
                }
            }
            return newEventAttributeArray;
        }

        private void OnEnable()
        {
            s_RegisteredInspector.Add((target as VisualEffectControlPlayableAsset, this));

            scrubbingProperty = serializedObject.FindProperty(nameof(VisualEffectControlPlayableAsset.scrubbing));
            startSeedProperty = serializedObject.FindProperty(nameof(VisualEffectControlPlayableAsset.startSeed));

            var clipEventsProperty = serializedObject.FindProperty(nameof(VisualEffectControlPlayableAsset.clipEvents));
            var singleEventsProperty = serializedObject.FindProperty(nameof(VisualEffectControlPlayableAsset.singleEvents));

            m_ReoderableClipEvents = BuildEventReordableList(serializedObject, clipEventsProperty);
            m_ReoderableClipEvents.drawHeaderCallback += (Rect r) => { EditorGUI.LabelField(r, "Clip Events"); };
            m_ReoderableClipEvents.onAddCallback = (ReorderableList list) =>
            {
                var playable = clipEventsProperty.serializedObject.targetObject as VisualEffectControlPlayableAsset;
                Undo.RegisterCompleteObjectUndo(playable, "Add new clip event");
                clipEventsProperty.serializedObject.ApplyModifiedProperties();

                var newClipEvent = new VisualEffectControlPlayableAsset.ClipEvent();
                if (playable.clipEvents.Any())
                {
                    var last = playable.clipEvents.Last();
                    newClipEvent = last;
                    newClipEvent.enter.eventAttributes = new UnityEngine.VFX.EventAttributes()
                    {
                        content = DeepClone(last.enter.eventAttributes.content)
                    };
                    newClipEvent.exit.eventAttributes = new UnityEngine.VFX.EventAttributes()
                    {
                        content = DeepClone(last.exit.eventAttributes.content)
                    };
                }
                else
                {
                    newClipEvent.enter.eventAttributes = new UnityEngine.VFX.EventAttributes();
                    newClipEvent.enter.name = VisualEffectAsset.PlayEventName;
                    newClipEvent.enter.time = 0.0;
                    newClipEvent.enter.timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart;

                    newClipEvent.exit.eventAttributes = new UnityEngine.VFX.EventAttributes();
                    newClipEvent.exit.name = VisualEffectAsset.StopEventName;
                    newClipEvent.exit.time = 0.0;
                    newClipEvent.exit.timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd;
                }
                playable.clipEvents.Add(newClipEvent);
                clipEventsProperty.serializedObject.Update();
            };

            m_ReoderableSingleEvents = BuildEventReordableList(serializedObject, singleEventsProperty);
            m_ReoderableSingleEvents.drawHeaderCallback += (Rect r) => { EditorGUI.LabelField(r, "Single Events"); };
            m_ReoderableSingleEvents.onAddCallback = (ReorderableList list) =>
            {
                var playable = singleEventsProperty.serializedObject.targetObject as VisualEffectControlPlayableAsset;
                Undo.RegisterCompleteObjectUndo(playable, "Add new single event");
                singleEventsProperty.serializedObject.ApplyModifiedProperties();

                var newSingleEvent = new VisualEffectPlayableSerializedEvent();
                if (playable.singleEvents.Any())
                {
                    var last = playable.singleEvents.Last();
                    newSingleEvent = last;
                    newSingleEvent.eventAttributes = new UnityEngine.VFX.EventAttributes()
                    {
                        content = DeepClone(last.eventAttributes.content)
                    };
                }
                playable.singleEvents.Add(newSingleEvent);
                singleEventsProperty.serializedObject.Update();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(scrubbingProperty);
            if (scrubbingProperty.boolValue)
                EditorGUILayout.PropertyField(startSeedProperty);

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
