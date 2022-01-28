using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public abstract class UIPropertyGenerator : IUIPropertyGenerator
    {
        [MustUseReturnValue]
        public abstract bool Generate(
            [DisallowNull] in UIDefinition.Property property,
            [NotNullWhen(true)] out UIImplementationIntermediateDocuments documents,
            [NotNullWhen(false)] out Exception error
        );
    }
}
