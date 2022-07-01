using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Unity uses this class to draw the user interface for all the settings
    /// contained in a <see cref="VolumeProfile"/> in the Inspector.
    /// </summary>
    /// <example>
    /// A minimal example of how to write a custom editor that displays the content of a profile
    /// in the inspector:
    /// <code>
    /// using UnityEngine.Rendering;
    ///
    /// [CustomEditor(typeof(VolumeProfile))]
    /// public class CustomVolumeProfileEditor : Editor
    /// {
    ///     VolumeComponentListEditor m_ComponentList;
    ///
    ///     void OnEnable()
    ///     {
    ///         m_ComponentList = new VolumeComponentListEditor(this);
    ///         m_ComponentList.Init(target as VolumeProfile, serializedObject);
    ///     }
    ///
    ///     void OnDisable()
    ///     {
    ///         if (m_ComponentList != null)
    ///             m_ComponentList.Clear();
    ///     }
    ///
    ///     public override void OnInspectorGUI()
    ///     {
    ///         serializedObject.Update();
    ///         m_ComponentList.OnGUI();
    ///         serializedObject.ApplyModifiedProperties();
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class VolumeComponentListEditor
    {
        /// <summary>
        /// A direct reference to the <see cref="VolumeProfile"/> this editor displays.
        /// </summary>
        public VolumeProfile asset { get; private set; }

        /// <summary>
        /// Obtains if all the volume components are visible
        /// </summary>
        internal bool hasHiddenVolumeComponents => m_Editors.Count != asset.components.Count;

        Editor m_BaseEditor;

        SerializedObject m_SerializedObject;
        SerializedProperty m_ComponentsProperty;

        List<VolumeComponentEditor> m_Editors = new List<VolumeComponentEditor>();

        /// <summary>
        /// Creates a new instance of <see cref="VolumeComponentListEditor"/> to use in an
        /// existing editor.
        /// </summary>
        /// <param name="editor">A reference to the parent editor instance</param>
        public VolumeComponentListEditor(Editor editor)
        {
            Assert.IsNotNull(editor);
            m_BaseEditor = editor;
        }

        /// <summary>
        /// Initializes the editor.
        /// </summary>
        /// <param name="asset">A direct reference to the profile Asset.</param>
        /// <param name="serializedObject">An instance of the <see cref="SerializedObject"/>
        /// provided by the parent editor.</param>
        public void Init(VolumeProfile asset, SerializedObject serializedObject)
        {
            Assert.IsNotNull(asset);
            Assert.IsNotNull(serializedObject);

            this.asset = asset;
            m_SerializedObject = serializedObject;

            RefreshEditors();

            // Keep track of undo/redo to redraw the inspector when that happens
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            asset.isDirty = true;

            // Dumb hack to make sure the serialized object is up to date on undo (else there'll be
            // a state mismatch when this class is used in a GameObject inspector).
            if (m_SerializedObject != null
                && !m_SerializedObject.Equals(null)
                && m_SerializedObject.targetObject != null
                && !m_SerializedObject.targetObject.Equals(null))
            {
                m_SerializedObject.Update();
                m_SerializedObject.ApplyModifiedProperties();
            }

            // Seems like there's an issue with the inspector not repainting after some undo events
            // This will take care of that
            m_BaseEditor.Repaint();
        }

        // index is only used when we need to re-create a component in a specific spot (e.g. reset)
        void CreateEditor(VolumeComponent component, SerializedProperty property, int index = -1, bool forceOpen = false)
        {
            var editor = (VolumeComponentEditor)Editor.CreateEditor(component);
            editor.inspector = m_BaseEditor;
            editor.Init();

            if (forceOpen)
                editor.expanded = true;

            if (index < 0)
                m_Editors.Add(editor);
            else
                m_Editors[index] = editor;
        }

        int m_CurrentHashCode;

        void ClearEditors()
        {
            if (m_Editors?.Any() ?? false)
            {
                // Disable all editors first
                foreach (var editor in m_Editors)
                    UnityEngine.Object.DestroyImmediate(editor);

                // Remove them
                m_Editors.Clear();
            }
        }

        void RefreshEditors()
        {
            ClearEditors();

            // Refresh the ref to the serialized components in case the asset got swapped or another
            // script is editing it while it's active in the inspector
            m_SerializedObject.Update();
            m_ComponentsProperty = m_SerializedObject.Find((VolumeProfile x) => x.components);
            Assert.IsNotNull(m_ComponentsProperty);

            // Recreate editors for existing settings, if any
            var components = asset.components;
            for (int i = 0; i < components.Count; i++)
                CreateEditor(components[i], m_ComponentsProperty.GetArrayElementAtIndex(i));

            m_CurrentHashCode = asset.GetComponentListHashCode();
        }

        /// <summary>
        /// Cleans up the editor and individual <see cref="VolumeComponentEditor"/> instances. You
        /// must call this when the parent editor is disabled or destroyed.
        /// </summary>
        public void Clear()
        {
            ClearEditors();
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        /// <summary>
        /// Draws the editor.
        /// </summary>
        public void OnGUI()
        {
            if (asset == null)
                return;

            // Even if the asset is not dirty, the list of component may have been changed by another inspector.
            // In this case, only the hash will tell us that we need to refresh.
            if (asset.isDirty || asset.GetComponentListHashCode() != m_CurrentHashCode)
            {
                RefreshEditors();
                asset.isDirty = false;
            }

            bool isEditable = !VersionControl.Provider.isActive
                || AssetDatabase.IsOpenForEdit(asset, StatusQueryOptions.UseCachedIfPossible);

            using (new EditorGUI.DisabledScope(!isEditable))
            {
                for (int i = 0; i < m_Editors.Count; i++)
                {
                    var editor = m_Editors[i];
                    if (!editor.visible)
                        continue;

                    var title = editor.GetDisplayTitle();
                    int id = i; // Needed for closure capture below

                    CoreEditorUtils.DrawSplitter();
                    bool displayContent = CoreEditorUtils.DrawHeaderToggleFoldout(
                        title,
                        editor.expanded,
                        editor.activeProperty,
                        pos => OnContextClick(pos, editor, id),
                        editor.hasAdditionalProperties ? () => editor.showAdditionalProperties : (Func<bool>)null,
                        () => editor.showAdditionalProperties ^= true,
                        Help.GetHelpURLForObject(editor.volumeComponent)
                    );

                    if (displayContent ^ editor.expanded)
                        editor.expanded = displayContent;

                    if (editor.expanded)
                    {
                        using (new EditorGUI.DisabledScope(!editor.activeProperty.boolValue))
                            editor.OnInternalInspectorGUI();
                    }
                }

                if (m_Editors.Count > 0)
                    CoreEditorUtils.DrawSplitter();
                else
                    EditorGUILayout.HelpBox("This Volume Profile contains no overrides.", MessageType.Info);

                EditorGUILayout.Space();

                using (var hscope = new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("Add Override"), EditorStyles.miniButton))
                    {
                        var r = hscope.rect;
                        var pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
                        FilterWindow.Show(pos, new VolumeComponentProvider(asset, this));
                    }
                }
            }
        }

        void OnContextClick(Vector2 position, VolumeComponentEditor targetEditor, int id)
        {
            var targetComponent = targetEditor.volumeComponent;
            var menu = new GenericMenu();

            menu.AddItem(EditorGUIUtility.TrTextContent("Move to Top"), false, () => MoveComponent(id, Move.Top));
            menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(id, Move.Up));
            menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, Move.Down));
            menu.AddItem(EditorGUIUtility.TrTextContent("Move to Bottom"), false, () => MoveComponent(id, Move.Bottom));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All"), false, () => CollapseComponents());
            menu.AddItem(EditorGUIUtility.TrTextContent("Expand All"), false, () => ExpandComponents());
            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Reset"), false, () => ResetComponent(targetComponent.GetType(), id));
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));
            menu.AddSeparator(string.Empty);
            if (targetEditor.hasAdditionalProperties)
                menu.AddItem(EditorGUIUtility.TrTextContent("Show Additional Properties"), targetEditor.showAdditionalProperties, () => targetEditor.showAdditionalProperties ^= true);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Show Additional Properties"));
            menu.AddItem(EditorGUIUtility.TrTextContent("Show All Additional Properties..."), false, () => CoreRenderPipelinePreferences.Open());

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Copy Settings"), false, () => CopySettings(targetComponent));

            if (CanPaste(targetComponent))
                menu.AddItem(EditorGUIUtility.TrTextContent("Paste Settings"), false, () => PasteSettings(targetComponent));
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Paste Settings"));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Toggle All"), false, () => m_Editors[id].SetAllOverridesTo(true));
            menu.AddItem(EditorGUIUtility.TrTextContent("Toggle None"), false, () => m_Editors[id].SetAllOverridesTo(false));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        VolumeComponent CreateNewComponent(Type type)
        {
            var effect = (VolumeComponent)ScriptableObject.CreateInstance(type);
            effect.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            effect.name = type.Name;
            return effect;
        }

        internal void AddComponent(Type type)
        {
            m_SerializedObject.Update();

            var component = CreateNewComponent(type);
            Undo.RegisterCreatedObjectUndo(component, "Add Volume Override");

            // Store this new effect as a subasset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(asset))
                AssetDatabase.AddObjectToAsset(component, asset);

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_ComponentsProperty.arraySize++;
            var componentProp = m_ComponentsProperty.GetArrayElementAtIndex(m_ComponentsProperty.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Create & store the internal editor object for this effect
            CreateEditor(component, componentProp, forceOpen: true);

            m_SerializedObject.ApplyModifiedProperties();

            // Force save / refresh
            if (EditorUtility.IsPersistent(asset))
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
        }

        internal void RemoveComponent(int id)
        {
            // Remove from the cached editors list
            UnityEngine.Object.DestroyImmediate(m_Editors[id]);
            m_Editors.RemoveAt(id);

            m_SerializedObject.Update();

            var property = m_ComponentsProperty.GetArrayElementAtIndex(id);
            var component = property.objectReferenceValue;

            // Unassign it (should be null already but serialization does funky things
            property.objectReferenceValue = null;

            // ...and remove the array index itself from the list
            m_ComponentsProperty.DeleteArrayElementAtIndex(id);

            m_SerializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            Undo.DestroyObjectImmediate(component);

            // Force save / refresh
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        // Reset is done by deleting and removing the object from the list and adding a new one in
        // the same spot as it was before
        internal void ResetComponent(Type type, int id)
        {
            // Remove from the cached editors list
            UnityEngine.Object.DestroyImmediate(m_Editors[id]);
            m_Editors[id] = null;

            m_SerializedObject.Update();

            var property = m_ComponentsProperty.GetArrayElementAtIndex(id);
            var prevComponent = property.objectReferenceValue;

            // Unassign it but down remove it from the array to keep the index available
            property.objectReferenceValue = null;

            // Create a new object
            var newComponent = CreateNewComponent(type);
            Undo.RegisterCreatedObjectUndo(newComponent, "Reset Volume Overrides");

            // Store this new effect as a subasset so we can reference it safely afterwards
            AssetDatabase.AddObjectToAsset(newComponent, asset);

            // Put it in the reserved space
            property.objectReferenceValue = newComponent;

            // Create & store the internal editor object for this effect
            CreateEditor(newComponent, property, id);

            m_SerializedObject.ApplyModifiedProperties();

            // Same as RemoveComponent, destroy at the end so it's recreated first on Undo to make
            // sure the GUID exists before undoing the list state
            Undo.DestroyObjectImmediate(prevComponent);

            // Force save / refresh
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        internal enum Move
        {
            Up,
            Down,
            Top,
            Bottom
        }

        internal void MoveComponent(int id, Move move)
        {
            m_SerializedObject.Update();

            int newIndex = id;

            // Find the index based on the visible editor
            switch(move)
            {
                case Move.Up:
                    {
                        do
                        {
                            newIndex--;
                        }
                        while (newIndex >= 0 && !m_Editors[newIndex].visible);
                    }
                    break;
                case Move.Down:
                    {
                        do
                        {
                            newIndex++;
                        }
                        while (newIndex < m_Editors.Count && !m_Editors[newIndex].visible);
                    }
                    break;
                case Move.Top:
                    newIndex = 0;
                    break;
                case Move.Bottom:
                    newIndex = m_Editors.Count - 1;
                    break;
            }

            newIndex = Mathf.Clamp(newIndex, 0, m_Editors.Count - 1);

            m_ComponentsProperty.MoveArrayElement(id, newIndex);
            m_SerializedObject.ApplyModifiedProperties();

            if (!m_Editors.TrySwap(id, newIndex, out var error))
                Debug.LogException(error);
        }

        internal void CollapseComponents()
        {
            // Move components
            m_SerializedObject.Update();
            int numEditors = m_Editors.Count;
            for (int i = 0; i < numEditors; ++i)
            {
                m_Editors[i].expanded = false;
            }
            m_SerializedObject.ApplyModifiedProperties();
        }

        internal void ExpandComponents()
        {
            // Move components
            m_SerializedObject.Update();
            int numEditors = m_Editors.Count;
            for (int i = 0; i < numEditors; ++i)
            {
                m_Editors[i].expanded = true;
            }
            m_SerializedObject.ApplyModifiedProperties();
        }

        static bool CanPaste(VolumeComponent targetComponent)
        {
            if (string.IsNullOrWhiteSpace(EditorGUIUtility.systemCopyBuffer))
                return false;

            string clipboard = EditorGUIUtility.systemCopyBuffer;
            int separator = clipboard.IndexOf('|');

            if (separator < 0)
                return false;

            return targetComponent.GetType().AssemblyQualifiedName == clipboard.Substring(0, separator);
        }

        static void CopySettings(VolumeComponent targetComponent)
        {
            string typeName = targetComponent.GetType().AssemblyQualifiedName;
            string typeData = JsonUtility.ToJson(targetComponent);
            EditorGUIUtility.systemCopyBuffer = $"{typeName}|{typeData}";
        }

        static void PasteSettings(VolumeComponent targetComponent)
        {
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            string typeData = clipboard.Substring(clipboard.IndexOf('|') + 1);
            Undo.RecordObject(targetComponent, "Paste Settings");
            JsonUtility.FromJsonOverwrite(typeData, targetComponent);
        }
    }
}
