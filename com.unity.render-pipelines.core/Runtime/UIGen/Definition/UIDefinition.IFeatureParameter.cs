using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public partial class UIDefinition
    {
        public interface IFeatureParameter
        {
            [MustUseReturnValue]
            bool Mutate([DisallowNull] Property property,
                [DisallowNull] ref UIImplementationIntermediateDocuments result,
                [NotNullWhen(false)] out Exception error);
        }
    }
}
