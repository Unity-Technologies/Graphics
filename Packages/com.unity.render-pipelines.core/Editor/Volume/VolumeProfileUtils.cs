using System;
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
        internal static void AssignValuesToProfile(VolumeComponent component, VolumeProfile profile)
        {
            var defaultComponent = GetVolumeComponentInProfile(profile, component.GetType());
            if (defaultComponent != null)
            {
                for (int i = 0; i < component.parameters.Count; i++)
                {
                    defaultComponent.parameters[i].SetValue(component.parameters[i]);
                }

                VolumeManager.instance.OnVolumeProfileChanged(profile);
            }
        }

        internal static void AssignValuesToDefaultProfile(VolumeComponent component, SerializedProperty newPropertyValue)
        {
            var defaultProfile = VolumeManager.instance.globalDefaultProfile;
            var defaultComponent = GetVolumeComponentInProfile(defaultProfile, component.GetType());
            if (defaultComponent != null)
            {
                var defaultObject = new SerializedObject(defaultComponent);
                var defaultProperty = defaultObject.FindProperty(newPropertyValue.propertyPath);
                if (defaultProperty != null)
                {
                    defaultProperty.serializedObject.CopyFromSerializedProperty(newPropertyValue);
                    defaultProperty.serializedObject.ApplyModifiedProperties();
                    VolumeManager.instance.OnVolumeProfileChanged(defaultProfile);
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
        /// Assign default and quality override profiles as default profiles in VolumeManager. Ensures that
        /// defaultVolumeProfile contains overrides for every component.
        /// </summary>
        /// <param name="globalDefaultVolumeProfile">VolumeProfile asset assigned in pipeline global settings.</param>
        /// <param name="defaultValueSource">An optional VolumeProfile asset containing default values to use for
        /// any components that are added to <see cref="globalDefaultVolumeProfile"/>.</param>
        public static void UpdateGlobalDefaultVolumeProfile(VolumeProfile globalDefaultVolumeProfile, VolumeProfile defaultValueSource = null)
        {
            Undo.RecordObject(globalDefaultVolumeProfile, $"Ensure {globalDefaultVolumeProfile.name} has all Volume Components");
            foreach (var comp in globalDefaultVolumeProfile.components)
                Undo.RecordObject(comp, $"Save {comp.name} state");

            EnsureOverridesForAllComponents(globalDefaultVolumeProfile, defaultValueSource);
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

        /// <summary>
        /// Ensure the provided VolumeProfile contains every VolumeComponent, they are active and overrideState for
        /// every VolumeParameter is true. Obsolete components are excluded.
        /// </summary>
        /// <param name="profile">VolumeProfile to use.</param>
        /// <param name="defaultValueSource">An optional VolumeProfile asset containing default values to use for
        /// any components that are added to <see cref="profile"/>.</param>
        public static void EnsureOverridesForAllComponents(VolumeProfile profile, VolumeProfile defaultValueSource = null)
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
            var volumeComponentTypes = VolumeManager.instance.baseComponentTypeArray;
            foreach (var type in volumeComponentTypes)
            {
                if (profile.components.All(x => x.GetType() != type))
                {
                    // Don't add obsolete or hidden components
                    if (type.IsDefined(typeof(ObsoleteAttribute), false) ||
                        type.IsDefined(typeof(HideInInspector), false))
                        continue;

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
            }

            if (changed)
            {
                VolumeManager.instance.OnVolumeProfileChanged(profile);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
