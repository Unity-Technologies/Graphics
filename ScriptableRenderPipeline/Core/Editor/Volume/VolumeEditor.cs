using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditor(typeof(Volume))]
    public sealed class VolumeEditor : Editor
    {
        SerializedProperty m_IsGlobal;
        SerializedProperty m_BlendRadius;
        SerializedProperty m_Weight;
        SerializedProperty m_Priority;
        SerializedProperty m_Components;

        Dictionary<Type, Type> m_EditorTypes; // Component type => Editor type
        List<VolumeComponentEditor> m_Editors;

        static VolumeComponent s_ClipboardContent;

        Volume actualTarget
        {
            get { return target as Volume; }
        }

        void OnEnable()
        {
            var o = new PropertyFetcher<Volume>(serializedObject);
            m_IsGlobal = o.Find(x => x.isGlobal);
            m_BlendRadius = o.Find(x => x.blendDistance);
            m_Weight = o.Find(x => x.weight);
            m_Priority = o.Find(x => x.priority);
            m_Components = o.Find(x => x.components);

            m_EditorTypes = new Dictionary<Type, Type>();
            m_Editors = new List<VolumeComponentEditor>();

            // Gets the list of all available component editors
            var editorTypes = CoreUtils.GetAllAssemblyTypes()
                                .Where(
                                    t => t.IsSubclassOf(typeof(VolumeComponentEditor))
                                      && t.IsDefined(typeof(VolumeComponentEditorAttribute), false)
                                      && !t.IsAbstract
                                );

            // Map them to their corresponding component type
            foreach (var editorType in editorTypes)
            {
                var attribute = (VolumeComponentEditorAttribute)editorType.GetCustomAttributes(typeof(VolumeComponentEditorAttribute), false)[0];
                m_EditorTypes.Add(attribute.componentType, editorType);
            }

            // Create editors for existing components
            var components = actualTarget.components;
            for (int i = 0; i < components.Count; i++)
                CreateEditor(components[i], m_Components.GetArrayElementAtIndex(i));

            // Keep track of undo/redo to redraw the inspector when that happens
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            if (m_Editors == null)
                return; // Hasn't been inited yet

            foreach (var editor in m_Editors)
                editor.OnDisable();

            m_Editors.Clear();
            m_EditorTypes.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (actualTarget.isDirty)
            {
                RefreshEditors();
                actualTarget.isDirty = false;
            }

            using (var scope = new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.PropertyField(m_IsGlobal);

                if (!m_IsGlobal.boolValue) // Blend radius is not needed for global volumes
                {
                    EditorGUILayout.PropertyField(m_BlendRadius);
                    m_BlendRadius.floatValue = Mathf.Max(m_BlendRadius.floatValue, 0f);
                }

                EditorGUILayout.PropertyField(m_Weight);
                EditorGUILayout.PropertyField(m_Priority);

                EditorGUILayout.Space();

                // Component list
                for (int i = 0; i < m_Editors.Count; i++)
                {
                    var editor = m_Editors[i];
                    string title = editor.GetDisplayTitle();
                    int id = i; // Needed for closure capture below

                    CoreEditorUtils.DrawSplitter();
                    bool displayContent = CoreEditorUtils.DrawHeaderToggle(
                        title,
                        editor.baseProperty,
                        editor.activeProperty,
                        pos => OnContextClick(pos, editor.target, id)
                    );

                    if (displayContent)
                    {
                        using (new EditorGUI.DisabledScope(!editor.activeProperty.boolValue))
                            editor.OnInternalInspectorGUI();
                    }
                }

                if (m_Editors.Count > 0)
                    CoreEditorUtils.DrawSplitter();
                else
                    EditorGUILayout.HelpBox("No override set on this volume.", MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    //if (GUILayout.Button(CoreEditorUtils.GetContent("Add Component"), GUILayout.Width(230f), GUILayout.Height(24f)))
                    //{

                    //}

                    GUILayout.FlexibleSpace();
                }

                // Handle components drag'n'drop
                var e = Event.current;
                if (e.type == EventType.DragUpdated)
                {
                    if (IsDragValid(scope.rect, e.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        e.Use();
                    }
                    else
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    }
                }
                else if (e.type == EventType.DragPerform)
                {
                    if (IsDragValid(scope.rect, e.mousePosition))
                    {
                        DragAndDrop.AcceptDrag();

                        var objs = DragAndDrop.objectReferences;
                        foreach (var o in objs)
                        {
                            var compType = ((MonoScript)o).GetClass();
                            AddComponent(compType);
                        }

                        e.Use();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        bool IsDragValid(Rect rect, Vector2 mousePos)
        {
            if (!rect.Contains(mousePos))
                return false;

            var objs = DragAndDrop.objectReferences;
            foreach (var o in objs)
            {
                if (o.GetType() != typeof(MonoScript))
                    return false;

                var script = (MonoScript)o;
                var scriptType = script.GetClass();

                if (!scriptType.IsSubclassOf(typeof(VolumeComponent)) || scriptType.IsAbstract)
                    return false;

                if (actualTarget.components.Exists(t => t.GetType() == scriptType))
                    return false;
            }

            return true;
        }

        void RefreshEditors()
        {
            // Disable all editors first
            foreach (var editor in m_Editors)
                editor.OnDisable();

            // Remove them
            m_Editors.Clear();

            // Recreate editors for existing settings, if any
            for (int i = 0; i < actualTarget.components.Count; i++)
                CreateEditor(actualTarget.components[i], m_Components.GetArrayElementAtIndex(i));
        }

        // index is only used when we need to re-create a component in a specific spot (e.g. reset)
        void CreateEditor(VolumeComponent settings, SerializedProperty property, int index = -1)
        {
            var settingsType = settings.GetType();
            Type editorType;

            if (!m_EditorTypes.TryGetValue(settingsType, out editorType))
                editorType = typeof(VolumeComponentEditor);

            var editor = (VolumeComponentEditor)Activator.CreateInstance(editorType);
            editor.Init(settings, this);
            editor.baseProperty = property.Copy();

            if (index < 0)
                m_Editors.Add(editor);
            else
                m_Editors[index] = editor;
        }

        void AddComponent(Type type)
        {
            serializedObject.Update();

            var component = (VolumeComponent)CreateInstance(type);
            Undo.RegisterCreatedObjectUndo(component, "Add Volume Component");

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_Components.arraySize++;
            var effectProp = m_Components.GetArrayElementAtIndex(m_Components.arraySize - 1);
            effectProp.objectReferenceValue = component;

            // Create & store the internal editor object for this effect
            CreateEditor(component, effectProp);

            serializedObject.ApplyModifiedProperties();
        }

        void RemoveComponent(int id)
        {
            // Huh. Hack to keep foldout state on the next element...
            bool nextFoldoutState = false;
            if (id < m_Editors.Count - 1)
                nextFoldoutState = m_Editors[id + 1].baseProperty.isExpanded;

            // Remove from the cached editors list
            m_Editors[id].OnDisable();
            m_Editors.RemoveAt(id);

            serializedObject.Update();

            var property = m_Components.GetArrayElementAtIndex(id);
            var effect = property.objectReferenceValue;

            // Unassign it (should be null already but serialization does funky things
            property.objectReferenceValue = null;

            // ...and remove the array index itself from the list
            m_Components.DeleteArrayElementAtIndex(id);

            // Finally refresh editor reference to the serialized settings list
            for (int i = 0; i < m_Editors.Count; i++)
                m_Editors[i].baseProperty = m_Components.GetArrayElementAtIndex(i).Copy();

            // Set the proper foldout state if needed
            if (id < m_Editors.Count)
                m_Editors[id].baseProperty.isExpanded = nextFoldoutState;

            serializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            Undo.DestroyObjectImmediate(effect);
        }

        // Reset is done by deleting and removing the object from the list and adding a new one in
        // the same spot as it was before
        void ResetComponent(Type type, int id)
        {
            // Remove from the cached editors list
            m_Editors[id].OnDisable();
            m_Editors[id] = null;

            serializedObject.Update();

            var property = m_Components.GetArrayElementAtIndex(id);
            var prevSettings = property.objectReferenceValue;

            // Unassign it but down remove it from the array to keep the index available
            property.objectReferenceValue = null;

            // Create a new object
            var newEffect = (VolumeComponent)CreateInstance(type);
            Undo.RegisterCreatedObjectUndo(newEffect, "Reset Volume Component");

            // Put it in the reserved space
            property.objectReferenceValue = newEffect;

            // Create & store the internal editor object for this effect
            CreateEditor(newEffect, property, id);

            serializedObject.ApplyModifiedProperties();

            // Same as RemoveComponent, destroy at the end so it's recreated first on Undo to make
            // sure the GUID exists before undoing the list state
            Undo.DestroyObjectImmediate(prevSettings);
        }

        void MoveComponent(int id, int offset)
        {
            // Move components
            serializedObject.Update();
            m_Components.MoveArrayElement(id, id + offset);
            serializedObject.ApplyModifiedProperties();

            // Move editors
            var prev = m_Editors[id + offset];
            m_Editors[id + offset] = m_Editors[id];
            m_Editors[id] = prev;
        }

        void OnContextClick(Vector2 position, VolumeComponent targetComponent, int id)
        {
            var menu = new GenericMenu();

            if (id == 0)
                menu.AddDisabledItem(CoreEditorUtils.GetContent("Move Up"));
            else
                menu.AddItem(CoreEditorUtils.GetContent("Move Up"), false, () => MoveComponent(id, -1));

            if (id == m_Editors.Count - 1)
                menu.AddDisabledItem(CoreEditorUtils.GetContent("Move Down"));
            else
                menu.AddItem(CoreEditorUtils.GetContent("Move Down"), false, () => MoveComponent(id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(CoreEditorUtils.GetContent("Reset"), false, () => ResetComponent(targetComponent.GetType(), id));
            menu.AddItem(CoreEditorUtils.GetContent("Remove"), false, () => RemoveComponent(id));
            menu.AddSeparator(string.Empty);
            menu.AddItem(CoreEditorUtils.GetContent("Copy Settings"), false, () => CopySettings(targetComponent));

            if (CanPaste(targetComponent))
                menu.AddItem(CoreEditorUtils.GetContent("Paste Settings"), false, () => PasteSettings(targetComponent));
            else
                menu.AddDisabledItem(CoreEditorUtils.GetContent("Paste Settings"));

            menu.AddSeparator(string.Empty);
            menu.AddItem(CoreEditorUtils.GetContent("Toggle All"), false, () => m_Editors[id].SetAllOverridesTo(true));
            menu.AddItem(CoreEditorUtils.GetContent("Toggle None"), false, () => m_Editors[id].SetAllOverridesTo(false));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        // Copy/pasting is simply done by creating an in memory copy of the selected component and
        // copying over the serialized data to another; it doesn't use nor affect the OS clipboard
        bool CanPaste(VolumeComponent targetComponent)
        {
            return s_ClipboardContent != null
                && s_ClipboardContent.GetType() == targetComponent.GetType();
        }

        void CopySettings(VolumeComponent targetComponent)
        {
            if (s_ClipboardContent != null)
            {
                CoreUtils.Destroy(s_ClipboardContent);
                s_ClipboardContent = null;
            }

            s_ClipboardContent = (VolumeComponent)CreateInstance(targetComponent.GetType());
            EditorUtility.CopySerializedIfDifferent(targetComponent, s_ClipboardContent);
        }

        void PasteSettings(VolumeComponent targetComponent)
        {
            Assert.IsNotNull(s_ClipboardContent);
            Assert.AreEqual(s_ClipboardContent.GetType(), targetComponent.GetType());

            Undo.RecordObject(targetComponent, "Paste Settings");
            EditorUtility.CopySerializedIfDifferent(s_ClipboardContent, targetComponent);
        }

        void OnUndoRedoPerformed()
        {
            actualTarget.isDirty = true;

            // Dumb hack to make sure the serialized object is up to date on undo
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();

            // Seems like there's an issue with the inspector not repainting after some undo events
            // This will take care of that
            Repaint();
        }
    }
}
