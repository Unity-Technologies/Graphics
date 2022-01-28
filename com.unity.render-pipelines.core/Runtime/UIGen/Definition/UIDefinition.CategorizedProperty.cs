using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public class CategorizedProperty
        {
            [MustUseReturnValue]
            public static bool From(
                QualifiedCategory category,
                [DisallowNull] Property property,
                [NotNullWhen(true)] out CategorizedProperty categorizedProperty,
                [NotNullWhen(false)] out Exception error
            )
            {
                categorizedProperty = new CategorizedProperty(category, property);
                error = default;
                return true;
            }

            public readonly QualifiedCategory category;
            [System.Diagnostics.CodeAnalysis.NotNull]
            public Property property { get; }

            CategorizedProperty(QualifiedCategory category, [DisallowNull] Property property)
            {
                this.category = category;
                this.property = property;
            }

            public override string ToString()
            {
                return $"{category}: {property}";
            }
        }

        [MustUseReturnValue]
        public bool AddCategorizedProperty(
            PropertyPath path,
            [DisallowNull] Type type,
            PropertyName name,
            PropertyTooltip tooltip,
            CategoryId primaryCategory,
            CategoryId? secondaryCategory,
            [NotNullWhen(true)] out CategorizedProperty categorizedProperty,
            [NotNullWhen(false)] out Exception error
        )
        {
            categorizedProperty = default;

            if (!Property.New(path, type, out var property, out error))
                return false;

            if (!property.SetDisplayName(name, out error))
                return false;

            if (!property.SetTooltip(tooltip, out error))
                return false;

            if (!QualifiedCategory.From(primaryCategory, secondaryCategory, out var category, out error))
                return false;

            if (!CategorizedProperty.From(category, property, out categorizedProperty, out error))
                return false;

            categorizedProperties.list.Add(categorizedProperty);

            return true;
        }

        [MustUseReturnValue]
        public bool AddCategorizedProperty(
            QualifiedCategory category,
            [DisallowNull] Property property,
            [NotNullWhen(true)] out CategorizedProperty categorizedProperty,
            [NotNullWhen(false)] out Exception error
        )
        {
            if (!CategorizedProperty.From(category, property, out categorizedProperty, out error))
                return false;

            categorizedProperties.list.Add(categorizedProperty);

            return true;
        }
    }
}
