using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Utilities
{
    // Temporary helper class duplicated from GraphicsSettingsUtils.cs while better UXML localization support is not available.
    internal class LocalizationHelper
    {
        static void Localize(VisualElement visualElement, Func<VisualElement, string> get, Action<VisualElement, string> set)
        {
            var extractedText = get.Invoke(visualElement);
            if (string.IsNullOrWhiteSpace(extractedText))
                return;

            var localizedString = L10n.Tr(extractedText);
            set.Invoke(visualElement, localizedString);
        }

        internal static void LocalizeTooltip(VisualElement visualElement)
        {
            Localize(visualElement, e => e.tooltip, (e, s) => e.tooltip = s);
        }

        internal static void LocalizeText(Label label)
        {
            Localize(label, e => ((Label)e).text, (e, s) => ((Label)e).text = s);
        }

        internal static void LocalizePropertyField(PropertyField propertyField)
        {
            Localize(propertyField, e => ((PropertyField)e).label, (e, s) => ((PropertyField)e).label = s);
        }

        internal static void LocalizeVisualTree(VisualElement root)
        {
            root.Query<VisualElement>().ForEach(LocalizeTooltip);
            root.Query<PropertyField>().ForEach(LocalizePropertyField);
            root.Query<Label>().ForEach(label =>
            {
                if (label.ClassListContains("unity-object-field-display__label"))
                    return;
                LocalizeText(label);
            });
        }
    }
}
