using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public class UIDefinitionPropertyCategoryIndex : IDisposable
    {
        [MustUseReturnValue]
        public static bool FromDefinition(
            [DisallowNull] UIDefinition definition,
            [NotNullWhen(true)] out UIDefinitionPropertyCategoryIndex index,
            [NotNullWhen(false)] out Exception error
        )
        {
            index = new UIDefinitionPropertyCategoryIndex();
            error = default;
            return true;
        }

        public void Dispose() { }

        UIDefinitionPropertyCategoryIndex() { }
    }
}
