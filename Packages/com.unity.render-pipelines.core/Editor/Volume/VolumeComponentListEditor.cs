using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
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
    /// <para>A minimal example of how to write a custom editor that displays the content of a profile
    /// in the inspector:</para>
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
        internal bool hasHiddenVolumeComponents => asset != null && m_Editors.Count(e => e.visible) != asset.components.Count;

        Editor m_BaseEditor;

        SerializedObject m_SerializedObject;
        SerializedProperty m_ComponentsProperty;

        SearchField m_SearchField = new ();
        string m_SearchString = "";

        List<VolumeComponentEditor> m_Editors = new();

        Dictionary<string, List<VolumeComponentEditor>> m_EditorsByCategory = new();

        Dictionary<VolumeComponentEditor, string> m_VolumeComponentHelpUrls = new();

        bool m_IsDefaultVolumeProfile;

        /// <summary>List of all VolumeComponentEditors</summary>
        public List<VolumeComponentEditor> editors => m_Editors;

        /// <summary>
        /// Set whether the editor behaves as a default volume profile.
        /// </summary>
        /// <param name="isDefaultVolumeProfile">If set to true, the editor treats the volume profile as the default global profile; otherwise, it treats it as a custom profile.</param>
        public void SetIsGlobalDefaultVolumeProfile(bool isDefaultVolumeProfile)
        {
            m_IsDefaultVolumeProfile = isDefaultVolumeProfile;
            foreach (var editor in m_Editors)
                editor.enableOverrides = !isDefaultVolumeProfile;
        }

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

            VolumeManager.instance.OnVolumeProfileChanged(asset);
        }

        // index is only used when we need to re-create a component in a specific spot (e.g. reset)
        void CreateEditor(VolumeComponent component, SerializedProperty property, int index = -1,
            bool forceOpen = false)
        {
            var editor = (VolumeComponentEditor) Editor.CreateEditor(component);
            editor.SetVolume(m_BaseEditor.target as Volume); // May be null if we're editing the asset
            editor.SetVolumeProfile(asset);
            editor.enableOverrides = !m_IsDefaultVolumeProfile;
            editor.Init();

            if (forceOpen)
                editor.expanded = true;

            if (index < 0)
                m_Editors.Add(editor);
            else
                m_Editors[index] = editor;

            DocumentationUtils.TryGetHelpURL(component.GetType(), out string helpUrl);
            helpUrl ??= string.Empty;
            m_VolumeComponentHelpUrls[editor] = helpUrl;
        }

        void DetermineEditorsVisibility()
        {
            var currentRenderPipelineAssetType = GraphicsSettings.currentRenderPipelineAssetType;
            var currentRenderPipelineType = RenderPipelineManager.currentPipeline?.GetType();
            foreach (var editor in m_Editors)
            {
                editor.DetermineVisibility(currentRenderPipelineAssetType, currentRenderPipelineType);
            }
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

            m_EditorsByCategory.Clear();

            m_VolumeComponentHelpUrls.Clear();
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

            if (m_IsDefaultVolumeProfile && VolumeManager.instance.isInitialized)
                CreateEditorsByCategory();
        }

        /// <summary>
        /// Cleans up the editor and individual <see cref="VolumeComponentEditor"/> instances. You
        /// must call this when the parent editor is disabled or destroyed.
        /// </summary>
        public void Clear()
        {
            ClearEditors();
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            asset = null;
        }

        bool MatchesSearchString(string title)
        {
            return m_SearchString.Length == 0 || title.Contains(m_SearchString, StringComparison.OrdinalIgnoreCase);
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
            if (asset.dirtyState != VolumeProfile.DirtyState.None || asset.GetComponentListHashCode() != m_CurrentHashCode)
            {
                RefreshEditors();
                VolumeManager.instance.OnVolumeProfileChanged(asset);

                if ((asset.dirtyState & VolumeProfile.DirtyState.DirtyByProfileReset) != 0)
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                asset.dirtyState = VolumeProfile.DirtyState.None;
            }

            if (m_IsDefaultVolumeProfile && VolumeManager.instance.isInitialized && m_EditorsByCategory.Count == 0)
                CreateEditorsByCategory();

            DetermineEditorsVisibility();

            bool isEditable = !VersionControl.Provider.isActive
                              || AssetDatabase.IsOpenForEdit(asset, StatusQueryOptions.UseCachedIfPossible);

            using (new EditorGUI.DisabledScope(!isEditable))
            {
                bool profileDataChanged = false;

                bool ShouldDrawEditor(VolumeComponentEditor editor)
                {
                    if (!editor.visible)
                        return false;
                    return MatchesSearchString(editor.GetDisplayTitle().text);
                }

                void DrawEditor(VolumeComponentEditor editor, int index = -1)
                {
                    if (!ShouldDrawEditor(editor))
                        return;

                    var title = editor.GetDisplayTitle();
                    int id = index; // Needed for closure capture below

                    CoreEditorUtils.DrawSplitter();

                    var activeProperty = editor.activeProperty;
                    if (!editor.enableOverrides)
                        activeProperty = null;

                    bool wasActive = editor.activeProperty.boolValue;
                    bool displayContent = CoreEditorUtils.DrawHeaderToggleFoldout(
                        title,
                        editor.expanded,
                        activeProperty,
                        pos => OnContextClick(pos, editor, id),
                        editor.hasAdditionalProperties ? () => editor.showAdditionalProperties : (Func<bool>) null,
                        () => editor.showAdditionalProperties ^= true,
                        m_VolumeComponentHelpUrls[editor]
                    );

                    profileDataChanged |= wasActive != editor.activeProperty.boolValue;

                    if (displayContent ^ editor.expanded)
                        editor.expanded = displayContent;

                    if (editor.expanded)
                    {
                        using (new EditorGUI.DisabledScope(!editor.activeProperty.boolValue))
                        {
                            bool changed = editor.OnInternalInspectorGUI();
                            profileDataChanged |= changed;
                        }
                    }
                }

                if (m_IsDefaultVolumeProfile)
                {
                    Rect searchRect = GUILayoutUtility.GetRect(50, EditorGUIUtility.singleLineHeight);
                    searchRect.width -= 2;
                    m_SearchString = m_SearchField.OnGUI(searchRect, m_SearchString);
                    GUILayout.Space(2);

                    EditorGUILayout.HelpBox(
                        "The values in the Default Volume can be overridden by a Volume Profile assigned to SRP asset " +
                        "and Volumes inside scenes.", MessageType.Info);

                    // Default volume profile displays all components in fixed order arranged into categories
                    GUILayout.Space(8);
                    foreach (var kv in m_EditorsByCategory)
                    {
                        var category = kv.Key;
                        var editors = kv.Value;
                        if (editors.Count == 0)
                            continue;

                        bool allEditorsHiddenBySearch = true;
                        for (int i = 0; i < editors.Count; i++)
                        {
                            if (ShouldDrawEditor(editors[i]))
                            {
                                allEditorsHiddenBySearch = false;
                                break;
                            }
                        }

                        if (allEditorsHiddenBySearch)
                            continue; // Avoid drawing category header if nothing under it matches the search string

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(16);
                        GUILayout.Label(category, EditorStyles.boldLabel);
                        GUILayout.EndHorizontal();

                        for (int i = 0; i < editors.Count; i++)
                            DrawEditor(editors[i]);

                        GUILayout.Space(8);
                    }
                }
                else
                {
                    for (int i = 0; i < m_Editors.Count; i++)
                        DrawEditor(m_Editors[i], i);
                }

                if (!m_IsDefaultVolumeProfile)
                {
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

                if (profileDataChanged)
                    VolumeManager.instance.OnVolumeProfileChanged(asset);
            }
        }

        private void CreateEditorsByCategory()
        {
            var volumeComponentTypeList =
                VolumeManager.instance.GetVolumeComponentsForDisplay(GraphicsSettings.currentRenderPipelineAssetType);

            m_EditorsByCategory.Clear();

            // Initialize with some known categories in order to affect their order. Empty categories won't be displayed.
            m_EditorsByCategory.Add("Main", new List<VolumeComponentEditor>());
            m_EditorsByCategory.Add("Sky", new List<VolumeComponentEditor>());
            m_EditorsByCategory.Add("Lighting", new List<VolumeComponentEditor>());
            m_EditorsByCategory.Add("Shadowing", new List<VolumeComponentEditor>());
            m_EditorsByCategory.Add("Post-processing", new List<VolumeComponentEditor>());

            foreach (var editor in m_Editors)
            {
                bool isSupportedForDisplay = false;
                foreach (var kv in volumeComponentTypeList)
                {
                    if (kv.Item2 == editor.volumeComponent.GetType())
                    {
                        var path = kv.Item1;
                        var parts = path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 1)
                            editor.categoryTitle = "Main";
                        else
                            editor.categoryTitle = path.Substring(0, path.LastIndexOf('/')).Replace("/", " / ");
                        isSupportedForDisplay = true;
                        break;
                    }
                }

                if (isSupportedForDisplay)
                {
                    if (!m_EditorsByCategory.ContainsKey(editor.categoryTitle))
                        m_EditorsByCategory.Add(editor.categoryTitle, new List<VolumeComponentEditor>());
                    m_EditorsByCategory[editor.categoryTitle].Add(editor);
                }
            }

            foreach (var category in m_EditorsByCategory)
            {
                category.Value.Sort((a, b) => a.GetDisplayTitle().text.CompareTo(b.GetDisplayTitle().text));
            }
        }

        void OnContextClick(Vector2 position, VolumeComponentEditor targetEditor, int id)
        {
            var targetComponent = targetEditor.volumeComponent;
            var menu = new GenericMenu();

            if (!m_IsDefaultVolumeProfile)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Move to Top"), false, () => MoveComponent(id, Move.Top));
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(id, Move.Up));
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, Move.Down));
                menu.AddItem(EditorGUIUtility.TrTextContent("Move to Bottom"), false, () => MoveComponent(id, Move.Bottom));
                menu.AddSeparator(string.Empty);
            }

            menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All"), false, () => SetComponentEditorsExpanded(false));
            menu.AddItem(EditorGUIUtility.TrTextContent("Expand All"), false, () => SetComponentEditorsExpanded(true));
            menu.AddSeparator(string.Empty);

            menu.AddItem(EditorGUIUtility.TrTextContent("Reset"), false, () => ResetComponents(new []{ targetComponent }));
            if (!m_IsDefaultVolumeProfile)
                menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));


            if (targetEditor.hasAdditionalProperties)
            {
                menu.AddSeparator(string.Empty);
                menu.AddAdvancedPropertiesBoolMenuItem(() => targetEditor.showAdditionalProperties,
                                                       () => targetEditor.showAdditionalProperties ^= true);
            }

            targetEditor.AddDefaultProfileContextMenuEntries(menu, VolumeManager.instance.globalDefaultProfile,
                () => VolumeProfileUtils.CopyValuesToProfile(targetComponent, VolumeManager.instance.globalDefaultProfile));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Open In Rendering Debugger"), false,
                DebugDisplaySettingsVolume.OpenInRenderingDebugger);

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Copy Settings"), false, () =>
                VolumeComponentCopyPaste.CopySettings(targetComponent));

            if (VolumeComponentCopyPaste.CanPaste(targetComponent))
                menu.AddItem(EditorGUIUtility.TrTextContent("Paste Settings"), false, () =>
                {
                    VolumeComponentCopyPaste.PasteSettings(targetComponent);
                    VolumeManager.instance.OnVolumeProfileChanged(asset);
                });
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Paste Settings"));

            if (!m_IsDefaultVolumeProfile)
            {
                menu.AddSeparator(string.Empty);
                menu.AddItem(EditorGUIUtility.TrTextContent("Toggle All"), false,
                    () => m_Editors[id].SetAllOverridesTo(true));
                menu.AddItem(EditorGUIUtility.TrTextContent("Toggle None"), false,
                    () => m_Editors[id].SetAllOverridesTo(false));
            }

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        internal void AddComponent(Type type)
        {
            m_SerializedObject.Update();

            var component = VolumeProfileUtils.CreateNewComponent(type);
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

        internal void RemoveAllComponents()
        {
            List<UnityEngine.Object> components = new List<UnityEngine.Object>(m_ComponentsProperty.arraySize);
            for (int i = 0; i < m_ComponentsProperty.arraySize; i++)
                components.Add(m_ComponentsProperty.GetArrayElementAtIndex(i).objectReferenceValue);

            m_ComponentsProperty.ClearArray();
            m_SerializedObject.ApplyModifiedProperties();

            foreach (var component in components)
                Undo.DestroyObjectImmediate(component);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            RefreshEditors();
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

        void SetComponentEditorsExpanded(bool expanded)
        {
            VolumeProfileUtils.SetComponentEditorsExpanded(m_Editors, expanded);
        }

        internal void ResetAllComponents()
        {
            var components = new List<VolumeComponent>();
            foreach (var editor in m_Editors)
                components.Add(editor.volumeComponent);
            ResetComponents(components.ToArray());
        }

        void ResetComponents(VolumeComponent[] components)
        {
            // For default volume components are on by default, otherwise off
            bool defaultOverrideState = m_IsDefaultVolumeProfile;
            VolumeProfileUtils.ResetComponentsInternal(m_SerializedObject, asset, components, defaultOverrideState);
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
            switch (move)
            {
                case Move.Up:
                {
                    do
                    {
                        newIndex--;
                    } while (newIndex >= 0 && !m_Editors[newIndex].visible);
                }
                    break;
                case Move.Down:
                {
                    do
                    {
                        newIndex++;
                    } while (newIndex < m_Editors.Count && !m_Editors[newIndex].visible);
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
    }
}
