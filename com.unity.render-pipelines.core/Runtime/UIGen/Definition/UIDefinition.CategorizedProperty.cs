using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public class CategorizedProperty
        {
            QualifiedCategory category;
            Property property;
        }

        [MustUseReturnValue]
        public bool AddCategorizedProperty(
            PropertyPath path,
            [DisallowNull] Type type,
            PropertyName name,
            PropertyTooltip tooltip,
            [NotNullWhen(true)] out Property property,
            [NotNullWhen(false)] out Exception error
        )
        {
            if (!Property.New(path, type, out property, out error))
                return false;

            if (!property.AddFeature(new DisplayName(name), out error))
                return false;

            if (!property.AddFeature(new Tooltip(tooltip), out error))
                return false;

            return true;
        }
    }
}
