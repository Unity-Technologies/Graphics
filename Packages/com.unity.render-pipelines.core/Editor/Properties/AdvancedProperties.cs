using System;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Global accessor to advanced properties
    /// </summary>
    public static class AdvancedProperties
    {
        static class Keys
        {
            // TODO Deprecate this key in U7: Advanced properties were formerly called additional properties
            internal const string showAllAdditionalProperties = "General.ShowAllAdditionalProperties";
            internal const string advancedPropertiesMigrated = "General.LocalAdditionalPropertiesMigratedToGlobal";
            //END TODO

            internal const string showAdvancedProperties = "General.ShowAdvancedProperties";
        }

        // TODO Deprecate this in U7: Advanced properties were formerly called additional properties
        static AdvancedProperties()
        {
            // Migrate from the previous global state
            UpdateShowAdvancedProperties(Keys.showAllAdditionalProperties,
                EditorPrefs.HasKey(Keys.showAllAdditionalProperties) &&
                EditorPrefs.GetBool(Keys.showAllAdditionalProperties));
        }

        internal static void UpdateShowAdvancedProperties(string key, bool previousState)
        {
            if (previousState)
            {
                if (!EditorPrefs.HasKey(Keys.advancedPropertiesMigrated) || !EditorPrefs.GetBool(Keys.advancedPropertiesMigrated))
                {
                    // Before we were storing a global state and a per editor state.
                    // So if the user had at least 1 editor with show additional, we need to show advanced properties everywhere.
                    enabled = true;
                    EditorPrefs.SetBool(Keys.advancedPropertiesMigrated, true);
                }
            }

            if (EditorPrefs.HasKey(key))
                EditorPrefs.DeleteKey(key);
        }
        // END TODO

        /// <summary>
        /// Global event when the advanced preferences have changed
        /// </summary>
        public static event Action<bool> advancedPreferenceChanged;

        private static bool? s_ShowAdvanced;

        /// <summary>
        /// If the show advanced properties is enabled
        /// </summary>
        public static bool enabled
        {
            get
            {
                s_ShowAdvanced ??= EditorPrefs.GetBool(Keys.showAdvancedProperties, false);
                return s_ShowAdvanced.Value;
            }
            set
            {
                if (s_ShowAdvanced != value)
                {
                    s_ShowAdvanced = value;
                    EditorPrefs.SetBool(Keys.showAdvancedProperties, value);
                    advancedPreferenceChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Adds an entry to toggle Advanced Properties
        /// </summary>
        /// <param name="menu">The menu where to add the Advanced Properties entry.</param>
        /// <param name="hasMoreOptions">If the option is checked</param>
        /// <param name="toggleMoreOptions">The toggle action</param>
        public static void AddAdvancedPropertiesBoolMenuItem(this GenericMenu menu, Func<bool> hasMoreOptions, Action toggleMoreOptions)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Advanced Properties"), hasMoreOptions.Invoke(), () => toggleMoreOptions.Invoke());
        }

        /// <summary>
        /// Adds an entry to toggle Advanced Properties
        /// </summary>
        /// <param name="menu">The menu where to add the Advanced Properties entry.</param>
        public static void AddAdvancedPropertiesBoolMenuItem(this GenericMenu menu)
        {
            AddAdvancedPropertiesBoolMenuItem(menu,
                () => AdvancedProperties.enabled,
                () => AdvancedProperties.enabled = !AdvancedProperties.enabled);
        }

        internal static AnimFloat s_AnimFloat = new(0)
        {
            speed = 0.2f
        };

        internal static void ResetHighlight()
        {
            s_AnimFloat.value = 1.0f;
            s_AnimFloat.target = 0.0f;
        }

        internal static bool IsHighlightActive() => s_AnimFloat.isAnimating;

        /// <summary>
        /// Starts the Advanced Properties highlight
        /// </summary>
        /// <param name="animation">The animation of the highlight. If null, the global animation value is used.</param>
        /// <returns>Tru, if the advanced properties is enabled</returns>
        public static bool BeginGroup(AnimFloat animation = null)
        {
            var oldColor = GUI.color;

            animation ??= s_AnimFloat;

            GUI.color = Color.Lerp(CoreEditorStyles.backgroundColor * oldColor, CoreEditorStyles.backgroundHighlightColor, animation.value);
            EditorGUILayout.BeginVertical(CoreEditorStyles.additionalPropertiesHighlightStyle);
            GUI.color = oldColor;

            return AdvancedProperties.enabled;
        }

        /// <summary>
        /// Ends the scope of highlight of advanced properties
        /// </summary>
        public static void EndGroup()
        {
            EditorGUILayout.EndVertical();
        }
    }
}
