using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.Rendering.HighDefinition
{
    static partial class StringExtention
    {
        /// <summary>Runtime alternative to UnityEditor.ObjectNames.NicifyVariableName. Only prefix 'm_' is not skipped.</summary>
        public static string CamelToPascalCaseWithSpace(this string text, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(char.ToUpper(text[0]));
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }
    }

    /// <summary>Should only be used on enum value of field to describe aspect in DebugMenu</summary>
    [AttributeUsage(AttributeTargets.Field)]
    class FrameSettingsFieldAttribute : Attribute
    {
        public enum DisplayType { BoolAsCheckbox, BoolAsEnumPopup, Others }
        public readonly DisplayType type;
        public readonly string displayedName;
        public readonly string tooltip;
        public readonly int group;
        public readonly int orderInGroup;
        public readonly Type targetType;
        public readonly int indentLevel;
        public readonly FrameSettingsField[] dependencies;
        private readonly int dependencySeparator;

        static int autoOrder = 0;

        private static Dictionary<FrameSettingsField, string> s_FrameSettingsEnumNameMap = null;

        public static Dictionary<FrameSettingsField, string> GetEnumNameMap()
        {
            if (s_FrameSettingsEnumNameMap == null)
            {
                s_FrameSettingsEnumNameMap = new Dictionary<FrameSettingsField, string>();
                Type type = typeof(FrameSettingsField);
                foreach (string enumName in Enum.GetNames(type))
                {
                    if (type.GetField(enumName).GetCustomAttribute<ObsoleteAttribute>() != null)
                        continue;

                    s_FrameSettingsEnumNameMap.Add((FrameSettingsField)Enum.Parse(type, enumName), enumName);
                }
            }

            return s_FrameSettingsEnumNameMap;
        }

        static FrameSettingsFieldAttribute()
        {
            GetEnumNameMap();//build the enum name map.
        }

        /// <summary>Attribute contenaing generation info for inspector and DebugMenu</summary>
        /// <param name="group">Group index contening this element.</param>
        /// <param name="autoName">[Optional] Helper to name the label as the enum entry given. Alternatively, use displayedName.</param>
        /// <param name="displayedName">[Optional] Displayed name to use as label. If given, it override autoname.</param>
        /// <param name="tooltip">[Optional] tooltip to use in inspector.</param>
        /// <param name="type">[Optional] Requested display for this entry.</param>
        /// <param name="targetType">[Requested for Enum] Allow to map boolean value to named enum.</param>
        /// <param name="positiveDependencies">[Optional] Dependencies that must be activated in order to appear activable in Inspector. Indentation is deduced frm this information.</param>
        /// <param name="customOrderInGroup">[Optional] If order is not the same than the order of value in the FrameSettingsField enum, you can ask to rewrite order from this element.
        /// Could be asked to use this on another element too to have correct ordering amongst everything.
        /// (Example if the 2nd element must be showed at position 10, add this property with value 10 to in and this parameter with value 3 to the following one (the 3rd).</param>
        public FrameSettingsFieldAttribute(
            int group,
            FrameSettingsField autoName = FrameSettingsField.None,
            string displayedName = null,
            string tooltip = null,
            DisplayType type = DisplayType.BoolAsCheckbox,
            Type targetType = null,
            FrameSettingsField[] positiveDependencies = null,
            FrameSettingsField[] negativeDependencies = null,
            int customOrderInGroup = -1)
        {
            if (string.IsNullOrEmpty(displayedName))
            {
                if (!s_FrameSettingsEnumNameMap.TryGetValue(autoName, out displayedName))
                {
                    displayedName = autoName.ToString();
                }
                displayedName = displayedName.CamelToPascalCaseWithSpace();
            }

            // Editor and Runtime debug menu
            this.group = group;
            if (customOrderInGroup != -1)
                autoOrder = customOrderInGroup; //start again numbering from this value
            this.orderInGroup = autoOrder++;
            this.displayedName = displayedName;
            this.type = type;
            this.targetType = targetType;
            dependencySeparator = positiveDependencies?.Length ?? 0;
            dependencies = new FrameSettingsField[dependencySeparator + (negativeDependencies?.Length ?? 0)];
            positiveDependencies?.CopyTo(dependencies, 0);
            negativeDependencies?.CopyTo(dependencies, dependencySeparator);
            indentLevel = dependencies?.Length ?? 0;

#if UNITY_EDITOR
            // Editor only
            this.tooltip = tooltip;
#endif
        }

        public bool IsNegativeDependency(FrameSettingsField frameSettingsField)
            => Array.FindIndex(dependencies, fsf => fsf == frameSettingsField) >= dependencySeparator;
    }
}
