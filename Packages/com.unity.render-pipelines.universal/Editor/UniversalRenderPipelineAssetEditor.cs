using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Styles = UnityEditor.Rendering.Universal.UniversalRenderPipelineAssetUI.Styles;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>UniversalRenderPipelineAsset</c> class.
    /// </summary>
    [CustomEditor(typeof(UniversalRenderPipelineAsset)), CanEditMultipleObjects]
    public class UniversalRenderPipelineAssetEditor : Editor
    {
        SerializedProperty m_RendererDataProp;
        SerializedProperty m_DefaultRendererProp;

        internal ReorderableList rendererList => m_RendererDataList;
        ReorderableList m_RendererDataList;

        private SerializedUniversalRenderPipelineAsset m_SerializedURPAsset;

#if ENABLE_UPSCALER_FRAMEWORK
        internal UpscalerOptionsEditorCache upscalerOptionsEditorCache => m_UpscalerOptionsEditorCache;
        UpscalerOptionsEditorCache m_UpscalerOptionsEditorCache;

        internal ReorderableList upscalerPriorityList => m_UpscalerPriorityList;
        ReorderableList m_UpscalerPriorityList;

        const float k_PlatformIconSize = 16f;
        const float k_PlatformIconSpacing = 2f;
        const float k_PlatformBadgeSize = 10f;

        struct UpscalerPlatform
        {
            public GUIContent content;
            public string supportedGraphicsAPIs;
            public string unsupportedGraphicsAPIs;
        }

        readonly Dictionary<string, UpscalerPlatform[]> m_UpscalerPlatforms = new(); // Keyed by upscaler ID

        // Foldout state per upscaler ID, so each upscaler's options can be expanded independently.
        internal readonly Dictionary<string, bool> m_UpscalerOptionsFoldoutStates = new();
#endif

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            m_SerializedURPAsset.Update();
            UniversalRenderPipelineAssetUI.Inspector.Draw(m_SerializedURPAsset, this);
            m_SerializedURPAsset.Apply();
        }

        void OnEnable()
        {
            m_SerializedURPAsset = new SerializedUniversalRenderPipelineAsset(serializedObject);

#if ENABLE_UPSCALER_FRAMEWORK
            RefreshUpscalerNames();
            m_UpscalerOptionsEditorCache = new UpscalerOptionsEditorCache();
            CacheUpscalerPlatforms();
            CreateUpscalerPriorityReorderableList();
            Undo.undoRedoPerformed += OnUndoRedoUpscalerPriority;
#endif

            CreateRendererReorderableList();
        }

        private void OnDisable()
        {
#if ENABLE_UPSCALER_FRAMEWORK
            Undo.undoRedoPerformed -= OnUndoRedoUpscalerPriority;
            m_UpscalerOptionsEditorCache?.Cleanup();
#endif
        }

        void CreateRendererReorderableList()
        {
            m_RendererDataProp = serializedObject.FindProperty("m_RendererDataList");
            m_DefaultRendererProp = serializedObject.FindProperty("m_DefaultRendererIndex");
            m_RendererDataList = new ReorderableList(serializedObject, m_RendererDataProp, true, true, true, true)
            {
                drawElementCallback = OnDrawElement,
                drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, Styles.rendererHeaderText),
                onCanRemoveCallback = reorderableList => reorderableList.count > 1,
                onRemoveCallback = OnRemoveElement,
                onReorderCallbackWithDetails = (reorderableList, index, newIndex) => UpdateDefaultRendererValue(index, newIndex) // Need to update the default renderer index
            };
        }

#if ENABLE_UPSCALER_FRAMEWORK
        void RefreshUpscalerNames()
        {
            // Every target shares one array here, so a refresh would write the first asset's names into all of them.
            if (serializedObject.isEditingMultipleObjects)
                return;

            SerializedProperty upscalerPriority = m_SerializedURPAsset.upscalerPriority;
            bool modified = false;

            for (int i = 0; i < upscalerPriority.arraySize; ++i)
            {
                SerializedProperty upscalerPriorityEntry = upscalerPriority.GetArrayElementAtIndex(i);
                string upscalerId = upscalerPriorityEntry.FindPropertyRelative("upscalerId").stringValue;

                if (!UpscalerRegistry.s_RegisteredUpscalers.TryGetValue(upscalerId, out var registered))
                    continue;

                SerializedProperty upscalerName = upscalerPriorityEntry.FindPropertyRelative("upscalerName");
                if (upscalerName.stringValue == registered.DisplayName)
                    continue;

                upscalerName.stringValue = registered.DisplayName;
                modified = true;
            }

            if (!modified)
                return;

            // Syncing a display-name cache isn't a user edit, so it stays off the undo stack.
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        void CreateUpscalerPriorityReorderableList()
        {
            m_UpscalerPriorityList = new ReorderableList(serializedObject, m_SerializedURPAsset.upscalerPriority, true, true, true, true)
            {
                drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, Styles.upscalerPriorityHeaderText),
                drawElementCallback = OnDrawUpscalerPriorityElement,
                onAddDropdownCallback = OnAddUpscalerPriorityDropdown,
                onRemoveCallback = OnRemoveUpscalerPriorityElement
            };
        }

        void OnDrawUpscalerPriorityElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;

            // The list sizes itself from the pending state, so index into that and not the applied asset.
            SerializedProperty upscalerPriorityEntry = m_SerializedURPAsset.upscalerPriority.GetArrayElementAtIndex(index);
            string upscalerId = upscalerPriorityEntry.FindPropertyRelative("upscalerId").stringValue;

            // Missing when the upscaler isn't registered, either because its package isn't installed or because it
            // compiles out for the active build target.
            bool isRegistered = m_UpscalerPlatforms.TryGetValue(upscalerId, out var platforms);

            string label;
            if (UpscalerRegistry.s_RegisteredUpscalers.TryGetValue(upscalerId, out var registered))
                label = registered.DisplayName;
            else
                label = upscalerPriorityEntry.FindPropertyRelative("upscalerName").stringValue;  // Use the serialized name as fallback

            // The badge overlays its platform icon.
            int iconCount = isRegistered ? platforms.Length : 1;
            float iconsWidth = iconCount * (k_PlatformIconSize + k_PlatformIconSpacing);

            float labelWidth = Mathf.Max(0f, rect.width - iconsWidth);
            var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);

            // An upscaler that is not registered will be visible but disabled.
            using (new EditorGUI.DisabledScope(!isRegistered))
                EditorGUI.LabelField(labelRect, label);

            float x = rect.xMax - iconsWidth;
            if (!isRegistered)
            {
                var warningRect = new Rect(x, rect.y, k_PlatformIconSize, k_PlatformIconSize);
                GUI.Label(warningRect, Styles.upscalerNotRegisteredWarning, EditorStyles.miniLabel);
                return;
            }

            foreach (var platform in platforms)
            {
                var iconRect = new Rect(x, rect.y, k_PlatformIconSize, k_PlatformIconSize);

                // A platform whose configured APIs are all unsupported reads as unavailable, like the label does.
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(platform.supportedGraphicsAPIs)))
                    GUI.Label(iconRect, platform.content, EditorStyles.miniLabel);

                // Drawn outside the scope so the badge stays legible over a disabled icon.
                if (!string.IsNullOrEmpty(platform.unsupportedGraphicsAPIs))
                {
                    var badgeRect = new Rect(iconRect.xMax - k_PlatformBadgeSize, iconRect.yMax - k_PlatformBadgeSize, k_PlatformBadgeSize, k_PlatformBadgeSize);
                    GUI.DrawTexture(badgeRect, Styles.upscalerGraphicsAPIBadge.image);
                }

                x += k_PlatformIconSize + k_PlatformIconSpacing;
            }
        }

        void OnAddUpscalerPriorityDropdown(Rect rect, ReorderableList list)
        {
            var menu = new GenericMenu();

            Type[] sortOrder = UniversalRenderPipeline.k_UpscalerSortOrder;
            var sorted = new (string id, string displayName)[sortOrder.Length];
            var unsorted = new List<(string id, string displayName)>();

            foreach (var upscaler in UpscalerRegistry.s_RegisteredUpscalers)
            {
                // Only offer upscalers that aren't in the list yet, so an upscaler can't be given two priorities.
                if (m_SerializedURPAsset.asset.IsUpscalerUsed(upscaler.Key))
                    continue;

                // Upscalers the pipeline declares no order for are listed after those it does.
                int index = Array.IndexOf(sortOrder, upscaler.Value.UpscalerType);
                if (index >= 0)
                    sorted[index] = (upscaler.Key, upscaler.Value.DisplayName);
                else
                    unsorted.Add((upscaler.Key, upscaler.Value.DisplayName));
            }

            foreach (var upscaler in sorted)
            {
                // Null where the upscaler isn't registered, or is already in the priority list.
                if (upscaler.id != null)
                    menu.AddItem(new GUIContent(upscaler.displayName), false, () => AddUpscalerToPriority(upscaler.id, upscaler.displayName));
            }

            // Registration order isn't defined, so name order is the only stable one available.
            unsorted.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase));
            foreach (var upscaler in unsorted)
                menu.AddItem(new GUIContent(upscaler.displayName), false, () => AddUpscalerToPriority(upscaler.id, upscaler.displayName));

            menu.AddSeparator(string.Empty);
            menu.AddItem(Styles.openPackageManagerText, false, () => PackageManager.UI.Window.Open(null));
            menu.DropDown(rect);
        }

        void AddUpscalerToPriority(string upscalerId, string upscalerName)
        {
            // Collapse entry and its options into a single undo step.
            int undoGroup = Undo.GetCurrentGroup();

            int index = m_SerializedURPAsset.upscalerPriority.arraySize;
            m_SerializedURPAsset.upscalerPriority.InsertArrayElementAtIndex(index);

            SerializedProperty upscalerPriorityEntry = m_SerializedURPAsset.upscalerPriority.GetArrayElementAtIndex(index);
            upscalerPriorityEntry.FindPropertyRelative("upscalerId").stringValue = upscalerId;
            upscalerPriorityEntry.FindPropertyRelative("upscalerName").stringValue = upscalerName;
            serializedObject.ApplyModifiedProperties();

            m_SerializedURPAsset.UpdateUpscalerOptions();

            Undo.SetCurrentGroupName($"Add {upscalerName} Upscaler");
            Undo.CollapseUndoOperations(undoGroup);
        }

        void OnRemoveUpscalerPriorityElement(ReorderableList list)
        {
            // Collapse entry and its options into a single undo step.
            int undoGroup = Undo.GetCurrentGroup();

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            serializedObject.ApplyModifiedProperties();
            m_SerializedURPAsset.UpdateUpscalerOptions();

            Undo.SetCurrentGroupName("Remove Upscaler");
            Undo.CollapseUndoOperations(undoGroup);
        }

        void OnUndoRedoUpscalerPriority()
        {
            // Options sub-assets are created and destroyed outside the undo stack.
            serializedObject.Update();
            m_SerializedURPAsset.UpdateUpscalerOptions();
            Repaint();
        }

        void CacheUpscalerPlatforms()
        {
            var installedPlatforms = GetInstalledPlatforms();
            m_UpscalerPlatforms.Clear();

            var upscalerPlatforms = new List<UpscalerPlatform>();
            var supportedAPINames = new List<string>();
            var unsupportedAPINames = new List<string>();
            foreach (var upscaler in UpscalerRegistry.s_RegisteredUpscalers)
            {
                upscalerPlatforms.Clear();
                foreach (var platform in installedPlatforms)
                {
                    // Build Profile Platforms
                    if (!UpscalerSupportedBuildTargetAttribute.IsBuildTargetSupported(upscaler.Value.UpscalerType, platform.buildTarget))
                        continue;

                    // Graphics APIs - Editing Player Settings doesn't reload the domain
                    supportedAPINames.Clear();
                    unsupportedAPINames.Clear();
                    for (int i = 0; i < platform.graphicsDeviceTypes.Length; ++i)
                    {
                        if (UpscalerSupportedBuildTargetAttribute.IsGraphicsAPISupported(upscaler.Value.UpscalerType, platform.buildTarget, platform.graphicsDeviceTypes[i]))
                            supportedAPINames.Add(platform.graphicsDeviceNames[i]);
                        else
                            unsupportedAPINames.Add(platform.graphicsDeviceNames[i]);
                    }

                    string supportedGraphicsAPIs = string.Join(", ", supportedAPINames);
                    string unsupportedGraphicsAPIs = string.Join(", ", unsupportedAPINames);

                    string tooltip = platform.buildTargetName;
                    if (supportedGraphicsAPIs.Length > 0)
                        tooltip += "\n" + string.Format(Styles.supportedGraphicsAPIs, supportedGraphicsAPIs);

                    if (unsupportedGraphicsAPIs.Length > 0)
                        tooltip += "\n" + string.Format(Styles.unsupportedGraphicsAPIs, unsupportedGraphicsAPIs);

                    // Falls back to a generic platform icon when the module ships none.
                    Texture icon = platform.icon;
                    if (icon == null)
                        icon = Styles.upscalerPlatformFallbackIcon.image;

                    var content = new GUIContent(icon, tooltip);

                    upscalerPlatforms.Add(new UpscalerPlatform
                    {
                        content = content,
                        supportedGraphicsAPIs = supportedGraphicsAPIs,
                        unsupportedGraphicsAPIs = unsupportedGraphicsAPIs
                    });
                }

                m_UpscalerPlatforms[upscaler.Key] = upscalerPlatforms.ToArray();
            }
        }

        static List<(BuildTarget buildTarget, string buildTargetName, Texture2D icon, GraphicsDeviceType[] graphicsDeviceTypes, string[] graphicsDeviceNames)> GetInstalledPlatforms()
        {
            var installedPlatforms = new List<(BuildTarget, string, Texture2D, GraphicsDeviceType[], string[])>();

            foreach (InstalledPlatformInfo platformInfo in BuildProfile.GetInstalledPlatformModules())
            {
                BuildTarget buildTarget = BuildTargetDiscoveryBridge.GetBuildTargetFromGUID(platformInfo.platformGuid);
                if (buildTarget == BuildTarget.NoTarget)
                    continue;

                Texture2D icon = BuildProfileModuleUtilBridge.GetPlatformIconSmall(platformInfo.platformGuid);

                // Resolves Auto Graphics API, so this is what the platform builds with either way.
                GraphicsDeviceType[] graphicsDeviceTypes = PlayerSettings.GetGraphicsAPIs(buildTarget);

                // Convert the Graphics API names once per platform rather than per tooltip.
                var graphicsDeviceNames = new string[graphicsDeviceTypes.Length];
                for (int i = 0; i < graphicsDeviceTypes.Length; ++i)
                    graphicsDeviceNames[i] = graphicsDeviceTypes[i].ToString();

                installedPlatforms.Add((buildTarget, platformInfo.displayName, icon, graphicsDeviceTypes, graphicsDeviceNames));
            }

            return installedPlatforms;
        }

#endif

        void OnRemoveElement(ReorderableList reorderableList)
        {
            bool shouldUpdateIndex = false;
            // Checking so that the user is not deleting  the default renderer
            if (reorderableList.index != m_DefaultRendererProp.intValue)
            {
                // Need to add the undo to the removal of our assets here, for it to work properly.
                Undo.RecordObject(target, $"Deleting renderer at index {reorderableList.index}");

                shouldUpdateIndex = true;
                m_RendererDataProp.DeleteArrayElementAtIndex(reorderableList.index);
            }
            else
            {
                EditorUtility.DisplayDialog(Styles.rendererListDefaultMessage.text, Styles.rendererListDefaultMessage.tooltip, "Close");
            }

            if (shouldUpdateIndex)
            {
                UpdateDefaultRendererValue(reorderableList.index);
            }

            EditorUtility.SetDirty(target);
        }

        void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2;
            var indexRect = new Rect(rect.x, rect.y, 14, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(indexRect, index.ToString());

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                var objectFieldWidth = rect.width - 110;
                var objRect = new Rect(rect.x + indexRect.width, rect.y, objectFieldWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(objRect, m_RendererDataProp.GetArrayElementAtIndex(index), GUIContent.none);
                if (changeCheck.changed)
                    EditorUtility.SetDirty(target);
            }

            var isDefaultRenderer = index == m_DefaultRendererProp.intValue;
            using (new EditorGUI.DisabledScope(isDefaultRenderer))
            {
                var defaultButtonX = rect.width - 51;
                var defaultButtonRect = new Rect(defaultButtonX, rect.y, 86, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(defaultButtonRect, !GUI.enabled ? Styles.rendererDefaultText : Styles.rendererSetDefaultText))
                {
                    m_DefaultRendererProp.intValue = index;
                    EditorUtility.SetDirty(target);
                }
            }

            // If object selector chose an object, assign it to the correct ScriptableRendererData slot.
            if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == index)
            {
                m_RendererDataProp.GetArrayElementAtIndex(index).objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
            }
        }

        void UpdateDefaultRendererValue(int index)
        {
            // If the index that is being removed is lower than the default renderer value,
            // the default prop value needs to be one lower.
            if (index < m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue--;
            }
        }

        void UpdateDefaultRendererValue(int prevIndex, int newIndex)
        {
            // If we are moving the index that is the same as the default renderer we need to update that
            if (prevIndex == m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue = newIndex;
            }
            // If newIndex is the same as default
            // then we need to know if newIndex is above or below the default index
            else if (newIndex == m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue += prevIndex > newIndex ? 1 : -1;
            }
            // If the old index is lower than default renderer and
            // the new index is higher then we need to move the default renderer index one lower
            else if (prevIndex < m_DefaultRendererProp.intValue && newIndex > m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue--;
            }
            else if (newIndex < m_DefaultRendererProp.intValue && prevIndex > m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue++;
            }
        }
    }
}
