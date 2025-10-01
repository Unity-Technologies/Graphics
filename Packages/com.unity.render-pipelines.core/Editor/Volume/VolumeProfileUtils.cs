using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Utility functions for handling VolumeProfiles in the editor.
    /// </summary>
    public static class VolumeProfileUtils
    {
        internal static class Styles
        {
            public static readonly GUIContent newVolumeProfile = EditorGUIUtility.TrTextContent("New Volume Profile...");
            public static readonly GUIContent clone = EditorGUIUtility.TrTextContent("Clone");
            public static readonly GUIContent collapseAll = EditorGUIUtility.TrTextContent("Collapse All");
            public static readonly GUIContent expandAll = EditorGUIUtility.TrTextContent("Expand All");
            public static readonly GUIContent reset = EditorGUIUtility.TrTextContent("Reset");
            public static readonly GUIContent resetAll = EditorGUIUtility.TrTextContent("Reset All");
            public static readonly GUIContent openInRenderingDebugger = EditorGUIUtility.TrTextContent("Open In Rendering Debugger");
            public static readonly GUIContent copySettings = EditorGUIUtility.TrTextContent("Copy Settings");
            public static readonly GUIContent copyAllSettings = EditorGUIUtility.TrTextContent("Copy All Settings");
            public static readonly GUIContent pasteSettings = EditorGUIUtility.TrTextContent("Paste Settings");
        }

        internal static void CopyValuesToProfile(VolumeComponent component, VolumeProfile profile)
        {
            var profileComponent = profile.GetVolumeComponentOfType(component.GetType());
            Undo.RecordObject(profileComponent, "Copy component to profile");
            CopyValuesToComponent(component, profileComponent, true);
            VolumeManager.instance.OnVolumeProfileChanged(profile);
        }

        internal static void CopyValuesToComponent(VolumeComponent component, VolumeComponent targetComponent, bool copyOnlyOverriddenParams)
        {
            if (targetComponent == null)
                return;

            for (int i = 0; i < component.parameters.Count; i++)
            {
                var param = component.parameters[i];
                if (copyOnlyOverriddenParams && !param.overrideState)
                    continue;
                var targetParam = targetComponent.parameters[i];
                targetParam.SetValue(param);
            }
        }

        internal static void AssignValuesToProfile(VolumeProfile targetProfile, VolumeComponent component, SerializedProperty newPropertyValue)
        {
            var defaultComponent = targetProfile.GetVolumeComponentOfType(component.GetType());
            if (defaultComponent != null)
            {
                var defaultObject = new SerializedObject(defaultComponent);
                var defaultProperty = defaultObject.FindProperty(newPropertyValue.propertyPath);
                if (defaultProperty != null)
                {
                    defaultProperty.serializedObject.CopyFromSerializedProperty(newPropertyValue);
                    defaultProperty.serializedObject.ApplyModifiedProperties();
                    VolumeManager.instance.OnVolumeProfileChanged(targetProfile);
                }
            }
        }

        /// <summary>
        /// Assign the global default default profile to VolumeManager. Ensures that defaultVolumeProfile contains
        /// overrides for every component. If defaultValueSource is provided, it will be used as the source for
        /// default values instead of default-constructing them.
        /// If components will be added to the profile, a confirmation dialog is displayed.
        /// </summary>
        /// <param name="globalDefaultVolumeProfile">VolumeProfile asset assigned in pipeline global settings.</param>
        /// <param name="defaultValueSource">An optional VolumeProfile asset containing default values to use for
        /// any components that are added to <see cref="globalDefaultVolumeProfile"/>.</param>
        /// <typeparam name="TRenderPipeline">The type of RenderPipeline that this VolumeProfile is used for. If it is
        /// not the active pipeline, the function does nothing.</typeparam>
        /// <returns>Whether the operation was confirmed</returns>
        public static bool UpdateGlobalDefaultVolumeProfileWithConfirmation<TRenderPipeline>(VolumeProfile globalDefaultVolumeProfile, VolumeProfile defaultValueSource = null)
            where TRenderPipeline : RenderPipeline
        {
            if (RenderPipelineManager.currentPipeline is not TRenderPipeline)
                return false;

            int numComponentsMissingFromProfile = GetTypesMissingFromDefaultProfile(globalDefaultVolumeProfile).Count;
            if (numComponentsMissingFromProfile == 0 ||
                EditorUtility.DisplayDialog(
                    "New Default Volume Profile",
                    $"Assigning {globalDefaultVolumeProfile.name} as the Default Volume Profile will add {numComponentsMissingFromProfile} Volume Components to it. Are you sure?", "Yes", "Cancel"))
            {
                UpdateGlobalDefaultVolumeProfile<TRenderPipeline>(globalDefaultVolumeProfile, defaultValueSource);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Assign the global default default profile to VolumeManager. Ensures that defaultVolumeProfile contains
        /// overrides for every component. If defaultValueSource is provided, it will be used as the source for
        /// default values instead of default-constructing them.
        /// </summary>
        /// <param name="globalDefaultVolumeProfile">VolumeProfile asset assigned in pipeline global settings.</param>
        /// <param name="defaultValueSource">An optional VolumeProfile asset containing default values to use for
        /// any components that are added to <see cref="globalDefaultVolumeProfile"/>.</param>
        /// <typeparam name="TRenderPipeline">The type of RenderPipeline that this VolumeProfile is used for. If it is
        /// not the active pipeline, the function does nothing.</typeparam>
        public static void UpdateGlobalDefaultVolumeProfile<TRenderPipeline>(VolumeProfile globalDefaultVolumeProfile, VolumeProfile defaultValueSource = null)
            where TRenderPipeline : RenderPipeline
        {
            if (RenderPipelineManager.currentPipeline is not TRenderPipeline)
                return;

            Undo.RecordObject(globalDefaultVolumeProfile, $"Ensure {globalDefaultVolumeProfile.name} has all Volume Components");
            foreach (var comp in globalDefaultVolumeProfile.components)
                Undo.RecordObject(comp, $"Save {comp.name} state");

            EnsureAllOverridesForDefaultProfile(globalDefaultVolumeProfile, defaultValueSource);
            VolumeManager.instance.SetGlobalDefaultProfile(globalDefaultVolumeProfile);
        }

        // Helper extension method: Returns the VolumeComponent of given type from the profile if present, or null
        static VolumeComponent GetVolumeComponentOfType(this VolumeProfile profile, Type type)
        {
            if (profile != null)
            {
                foreach (var component in profile.components)
                    if (component.GetType() == type)
                        return component;
            }
            return null;
        }

        // Helper extension method: Returns the VolumeComponent of given type from the profile if present, or a default-constructed one
        static VolumeComponent GetVolumeComponentOfTypeOrDefault(this VolumeProfile profile, Type type)
        {
            return profile.GetVolumeComponentOfType(type) ?? (VolumeComponent) ScriptableObject.CreateInstance(type);
        }

        static List<Type> GetTypesMissingFromDefaultProfile(VolumeProfile profile)
        {
            List<Type> missingTypes = new List<Type>();
            var volumeComponentTypes = VolumeManager.instance.baseComponentTypeArray;
            foreach (var type in volumeComponentTypes)
            {
                if (profile.components.Find(c => c.GetType() == type) == null)
                {
                    if (type.IsDefined(typeof(ObsoleteAttribute), false) ||
                        type.IsDefined(typeof(HideInInspector), false))
                        continue;

                    missingTypes.Add(type);
                }
            }

            return missingTypes;
        }

        /// <summary>
        /// Ensure the provided VolumeProfile contains every VolumeComponent, they are active and overrideState for
        /// every VolumeParameter is true. Obsolete and hidden components are excluded.
        /// </summary>
        /// <param name="profile">VolumeProfile to use.</param>
        /// <param name="defaultValueSource">An optional VolumeProfile asset containing default values to use for
        /// any components that are added to <see cref="profile"/>.</param>
        public static void EnsureAllOverridesForDefaultProfile(VolumeProfile profile, VolumeProfile defaultValueSource = null)
        {
            // It's possible that the volume profile is assigned to the default asset inside the HDRP package. In
            // this case it cannot be modified. User is expected to use HDRP Wizard "Fix" to create a local profile.
            var path = AssetDatabase.GetAssetPath(profile);
            if (CoreEditorUtils.IsAssetInReadOnlyPackage(path))
                return;

            bool changed = false;
            int numComponentsBefore = profile.components.Count;

            // Remove any obsolete VolumeComponents
            profile.components.RemoveAll(
                comp => comp == null || comp.GetType().IsDefined(typeof(ObsoleteAttribute), false));

            changed |= profile.components.Count != numComponentsBefore;

            // Ensure all existing VolumeComponents are active & all overrides enabled
            foreach (var comp in profile.components)
            {
                bool resetAll = false;
                if (!comp.active)
                {
                    changed = true;
                    comp.active = true;
                    resetAll = true;
                }

                VolumeComponent defaultValueComponent = null;
                for (int i = 0; i < comp.parameters.Count; ++i)
                {
                    var param = comp.parameters[i];
                    if (resetAll || !param.overrideState)
                    {
                        if (defaultValueComponent == null)
                            defaultValueComponent = defaultValueSource.GetVolumeComponentOfTypeOrDefault(comp.GetType());

                        // Because the parameter values for inactive VolumeComponents or non-overriden parameters are
                        // not in effect, we must reset these values to avoid unexpected changes when assigning an
                        // existing profile as a Default Profile.
                        param.SetValue(defaultValueComponent.parameters[i]);
                    }
                    if (!param.overrideState)
                    {
                        changed = true;
                        param.overrideState = true;
                    }
                }
            }

            // Add missing VolumeComponents to profile
            var missingTypes = GetTypesMissingFromDefaultProfile(profile);
            foreach (var type in missingTypes)
            {
                var comp = profile.Add(type, overrides: true);
                comp.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;

                // Copy values from default value source if present & overridden
                var defaultValueSourceComponent = defaultValueSource.GetVolumeComponentOfType(type);
                if (defaultValueSourceComponent != null)
                {
                    for (int i = 0; i < comp.parameters.Count; i++)
                    {
                        var defaultValueSourceParam = defaultValueSourceComponent.parameters[i];
                        if (defaultValueSourceParam.overrideState)
                            comp.parameters[i].SetValue(defaultValueSourceParam);
                    }
                }

                AssetDatabase.AddObjectToAsset(comp, profile);
                changed = true;
            }

            if (changed)
            {
                VolumeManager.instance.OnVolumeProfileChanged(profile);
                EditorUtility.SetDirty(profile);
            }
        }

        /// <summary>
        /// Adds context menu dropdown items for a Volume Profile.
        /// </summary>
        /// <param name="menu">Dropdown menu to add items to</param>
        /// <param name="volumeProfile">VolumeProfile associated with the context menu</param>
        /// <param name="componentEditors">List of VolumeComponentEditors associated with the profile</param>
        /// <param name="overrideStateOnReset">Default override state for components when they are reset</param>
        /// <param name="defaultVolumeProfilePath">Default path for the new volume profile&lt;</param>
        /// <param name="onNewVolumeProfileCreated">Callback when new volume profile has been created</param>
        /// <param name="onComponentEditorsExpandedCollapsed">Callback when all editors are collapsed or expanded</param>
        /// <param name="canCreateNewProfile">Whether it is allowed to create a new profile</param>
        public static void AddVolumeProfileContextMenuItems(
            ref GenericMenu menu,
            VolumeProfile volumeProfile,
            List<VolumeComponentEditor> componentEditors,
            bool overrideStateOnReset,
            string defaultVolumeProfilePath,
            Action<VolumeProfile> onNewVolumeProfileCreated,
            Action onComponentEditorsExpandedCollapsed = null,
            bool canCreateNewProfile = true)
        {
            if (canCreateNewProfile)
            {
                menu.AddItem(Styles.newVolumeProfile, false, () =>
                {
                    VolumeProfileFactory.CreateVolumeProfileWithCallback(defaultVolumeProfilePath,
                        onNewVolumeProfileCreated);
                });
            }
            else
            {
                menu.AddDisabledItem(Styles.newVolumeProfile, false);
            }

            if (volumeProfile != null)
            {
                if (canCreateNewProfile)
                {
                    menu.AddItem(Styles.clone, false, () =>
                    {
                        var pathName = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(volumeProfile));
                        var clone = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName, volumeProfile);
                        onNewVolumeProfileCreated(clone);
                    });
                }
                else
                {
                    menu.AddDisabledItem(Styles.clone, false);
                }

                menu.AddSeparator(string.Empty);

                menu.AddItem(Styles.collapseAll, false, () =>
                {
                    SetComponentEditorsExpanded(componentEditors, false);
                    onComponentEditorsExpandedCollapsed?.Invoke();
                });
                menu.AddItem(Styles.expandAll, false, () =>
                {
                    SetComponentEditorsExpanded(componentEditors, true);
                    onComponentEditorsExpandedCollapsed?.Invoke();
                });
            }

            menu.AddSeparator(string.Empty);

            menu.AddAdvancedPropertiesBoolMenuItem();

            menu.AddSeparator(string.Empty);

            menu.AddItem(Styles.openInRenderingDebugger, false, DebugDisplaySettingsVolume.OpenInRenderingDebugger);

            if (volumeProfile != null)
            {
                menu.AddSeparator(string.Empty);

                menu.AddItem(Styles.copyAllSettings, false,
                    () => VolumeComponentCopyPaste.CopySettings(volumeProfile.components));

                if (VolumeComponentCopyPaste.CanPaste(volumeProfile.components))
                    menu.AddItem(Styles.pasteSettings, false, () =>
                    {
                        VolumeComponentCopyPaste.PasteSettings(volumeProfile.components, volumeProfile);
                        VolumeManager.instance.OnVolumeProfileChanged(volumeProfile);
                    });
                else
                    menu.AddDisabledItem(Styles.pasteSettings, false);
            }
        }

        /// <summary>
        /// Draws the context menu dropdown for a Volume Profile.
        /// </summary>
        /// <param name="position">Context menu position</param>
        /// <param name="volumeProfile">VolumeProfile associated with the context menu</param>
        /// <param name="componentEditors">List of VolumeComponentEditors associated with the profile</param>
        /// <param name="defaultVolumeProfilePath">Default path for the new volume profile</param>
        /// <param name="overrideStateOnReset">Default override state for components when they are reset</param>
        /// <param name="onNewVolumeProfileCreated">Callback when new volume profile has been created</param>
        /// <param name="onComponentEditorsExpandedCollapsed">Callback when all editors are collapsed or expanded</param>
        public static void OnVolumeProfileContextClick(
            Vector2 position,
            VolumeProfile volumeProfile,
            List<VolumeComponentEditor> componentEditors,
            bool overrideStateOnReset,
            string defaultVolumeProfilePath,
            Action<VolumeProfile> onNewVolumeProfileCreated,
            Action onComponentEditorsExpandedCollapsed = null)
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.newVolumeProfile, false, () =>
            {
                VolumeProfileFactory.CreateVolumeProfileWithCallback(defaultVolumeProfilePath,
                    onNewVolumeProfileCreated);
            });

            if (volumeProfile != null)
            {
                menu.AddItem(Styles.clone, false, () =>
                {
                    var pathName = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(volumeProfile));
                    var clone = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName, volumeProfile);
                    onNewVolumeProfileCreated(clone);
                });

                menu.AddSeparator(string.Empty);

                menu.AddItem(Styles.collapseAll, false, () =>
                {
                    SetComponentEditorsExpanded(componentEditors, false);
                    onComponentEditorsExpandedCollapsed?.Invoke();
                });
                menu.AddItem(Styles.expandAll, false, () =>
                {
                    SetComponentEditorsExpanded(componentEditors, true);
                    onComponentEditorsExpandedCollapsed?.Invoke();
                });

                menu.AddSeparator(string.Empty);

                menu.AddItem(Styles.resetAll, false, () =>
                {
                    VolumeComponent[] components = new VolumeComponent[componentEditors.Count];
                    for (int i = 0; i < componentEditors.Count; i++)
                        components[i] = componentEditors[i].volumeComponent;

                    ResetComponentsInternal(new SerializedObject(volumeProfile), volumeProfile, components, overrideStateOnReset);
                });
            }

            menu.AddSeparator(string.Empty);

            menu.AddAdvancedPropertiesBoolMenuItem();

            menu.AddSeparator(string.Empty);

            menu.AddItem(Styles.openInRenderingDebugger, false, DebugDisplaySettingsVolume.OpenInRenderingDebugger);

            if (volumeProfile != null)
            {
                menu.AddSeparator(string.Empty);

                menu.AddItem(Styles.copyAllSettings, false,
                    () => VolumeComponentCopyPaste.CopySettings(volumeProfile.components));

                if (VolumeComponentCopyPaste.CanPaste(volumeProfile.components))
                    menu.AddItem(Styles.pasteSettings, false, () =>
                    {
                        VolumeComponentCopyPaste.PasteSettings(volumeProfile.components, volumeProfile);
                        VolumeManager.instance.OnVolumeProfileChanged(volumeProfile);
                    });
                else
                    menu.AddDisabledItem(Styles.pasteSettings);
            }

            menu.DropDown(new Rect(new Vector2(position.x, position.y), Vector2.zero));
        }

        internal static VolumeComponent CreateNewComponent(Type type)
        {
            var volumeComponent = (VolumeComponent) ScriptableObject.CreateInstance(type);
            volumeComponent.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            volumeComponent.name = type.Name;
            return volumeComponent;
        }

        internal static void ResetComponentsInternal(
            SerializedObject serializedObject,
            VolumeProfile asset,
            VolumeComponent[] components,
            bool newComponentDefaultOverrideState)
        {
            Undo.RecordObjects(components, "Reset All Volume Overrides");

            foreach (var targetComponent in components)
            {
                var newComponent = CreateNewComponent(targetComponent.GetType());
                CopyValuesToComponent(newComponent, targetComponent, false);
                targetComponent.SetAllOverridesTo(newComponentDefaultOverrideState);
            }

            serializedObject.ApplyModifiedProperties();

            VolumeManager.instance.OnVolumeProfileChanged(asset);

            // Force save / refresh
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        internal static void SetComponentEditorsExpanded(List<VolumeComponentEditor> editors, bool expanded)
        {
            foreach (var editor in editors)
            {
                editor.expanded = expanded;
            }
        }
    }
}
