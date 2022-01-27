using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public class UIContextDefinition
    {
        public struct Member
        {
            public Type type;
            public string name;
        }

        public List<Member> members { get; } = new List<Member>();
    }

    public static class UIContextDefinitionExtensions
    {
        [MustUseReturnValue]
        public static bool Aggregate<TList>(
            [DisallowNull] this TList definitions,
            [NotNullWhen(true)] out UIContextDefinition merged,
            [NotNullWhen(false)] out Exception error
        ) where TList : IList<UIContextDefinition>
        {
            throw new NotImplementedException();
        }
    }
}
