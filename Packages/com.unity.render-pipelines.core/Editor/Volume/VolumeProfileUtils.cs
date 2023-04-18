using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Utility functions for handling VolumeProfiles in the editor.
    /// </summary>
    public static class VolumeProfileUtils
    {
        internal static void CopyValuesToProfile(VolumeComponent component, VolumeProfile profile)
        {
            var profileComponent = GetVolumeComponentInProfile(profile, component.GetType());
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
            var defaultComponent = GetVolumeComponentInProfile(targetProfile, component.GetType());
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

        static VolumeComponent GetVolumeComponentInProfile(VolumeProfile profile, Type componentType)
        {
            if (profile != null)
            {
                return profile.components.FirstOrDefault(c => c.GetType() == componentType);
            }
            return null;
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
        /// <returns>Whether the operation was confirmed</returns>
        public static bool UpdateGlobalDefaultVolumeProfileWithConfirmation(VolumeProfile globalDefaultVolumeProfile, VolumeProfile defaultValueSource = null)
        {
            int numComponentsMissingFromProfile = GetTypesMissingFromDefaultProfile(globalDefaultVolumeProfile).Count;
            if (numComponentsMissingFromProfile == 0 ||
                EditorUtility.DisplayDialog(
                    "New Default Volume Profile",
                    $"Assigning {globalDefaultVolumeProfile.name} as the Default Volume Profile will add {numComponentsMissingFromProfile} Volume Components to it. Are you sure?", "Yes", "Cancel"))
            {
                UpdateGlobalDefaultVolumeProfile(globalDefaultVolumeProfile, defaultValueSource);
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
        public static void UpdateGlobalDefaultVolumeProfile(VolumeProfile globalDefaultVolumeProfile, VolumeProfile defaultValueSource = null)
        {
            Undo.RecordObject(globalDefaultVolumeProfile, $"Ensure {globalDefaultVolumeProfile.name} has all Volume Components");
            foreach (var comp in globalDefaultVolumeProfile.components)
                Undo.RecordObject(comp, $"Save {comp.name} state");

            EnsureAllOverridesForDefaultProfile(globalDefaultVolumeProfile, defaultValueSource);
            VolumeManager.instance.SetGlobalDefaultProfile(globalDefaultVolumeProfile);
        }

        // Helper extension method: Returns the VolumeComponent of given type from the profile if present, or null
        static VolumeComponent GetVolumeComponentOfType(this VolumeProfile profile, Type type)
        {
            if (profile == null)
                return null;
            return profile.components.FirstOrDefault(c => c.GetType() == type);
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
            if (!AssetDatabase.IsOpenForEdit(profile))
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
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Draws the context menu dropdown for a Volume Profile.
        /// </summary>
        /// <param name="position">Context menu position</param>
        /// <param name="editor">VolumeProfileEditor associated with the context menu</param>
        /// <param name="defaultVolumeProfilePath">Default path for the new volume profile</param>
        /// <param name="onNewVolumeProfileCreated">Callback when new volume profile has been created</param>
        public static void OnVolumeProfileContextClick(
            Vector2 position,
            VolumeProfileEditor editor,
            string defaultVolumeProfilePath,
            Action<VolumeProfile> onNewVolumeProfileCreated)
        {
            var volumeProfile = editor.target as VolumeProfile;
            if (volumeProfile == null)
                return;

            var menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("New Volume Profile..."), false, () =>
            {
                VolumeProfileFactory.CreateVolumeProfileWithCallback(defaultVolumeProfilePath,
                    onNewVolumeProfileCreated);
            });
            menu.AddItem(EditorGUIUtility.TrTextContent("Clone"), false, () =>
            {
                var pathName = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(volumeProfile));
                var clone = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName, volumeProfile);
                onNewVolumeProfileCreated(clone);
            });

            menu.AddSeparator(string.Empty);

            menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All"), false, editor.componentList.CollapseComponents);
            menu.AddItem(EditorGUIUtility.TrTextContent("Expand All"), false, editor.componentList.ExpandComponents);

            menu.AddSeparator(string.Empty);

            menu.AddItem(EditorGUIUtility.TrTextContent("Reset All"), false, editor.componentList.ResetComponents);

            menu.AddSeparator(string.Empty);

            menu.AddItem(EditorGUIUtility.TrTextContent("Show All Additional Properties..."), false,
                CoreRenderPipelinePreferences.Open);

            menu.AddSeparator(string.Empty);

            menu.AddItem(EditorGUIUtility.TrTextContent("Open In Rendering Debugger"), false,
                DebugDisplaySettingsVolume.OpenInRenderingDebugger);

            menu.AddSeparator(string.Empty);

            menu.AddItem(EditorGUIUtility.TrTextContent("Copy All Settings"), false,
                () => VolumeComponentCopyPaste.CopySettings(volumeProfile.components));

            if (VolumeComponentCopyPaste.CanPaste(volumeProfile.components))
                menu.AddItem(EditorGUIUtility.TrTextContent("Paste Settings"), false, () =>
                {
                    VolumeComponentCopyPaste.PasteSettings(volumeProfile.components);
                    VolumeManager.instance.OnVolumeProfileChanged(volumeProfile);
                });
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Paste Settings"));

            menu.DropDown(new Rect(new Vector2(position.x, position.y), Vector2.zero));
        }
    }
}
