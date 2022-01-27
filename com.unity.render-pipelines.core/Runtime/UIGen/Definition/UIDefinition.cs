using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    // TODO: [Fred] should be immutable with structs
    public partial class UIDefinition
    {
        // TODO: [Fred] should be readonly
        public PooledList<Property> properties = new();


        // Not Weird API, it is always confusing as for the direction of the data flow
        // Consider a static API
        /// <summary>
        /// Move the data from <paramref name="toMerge"/> into self.
        /// </summary>
        /// <param name="toMerge"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [MustUseReturnValue]
        public bool TakeAndMerge(
            [DisallowNull] UIDefinition toMerge,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }

    public static class UIDefinitionExtensions
    {
        [MustUseReturnValue]
        public static bool Aggregate<TList>(
            [DisallowNull] this TList definitions,
            [NotNullWhen(true)] out UIDefinition merged,
            [NotNullWhen(false)] out Exception error
        ) where TList : IList<UIDefinition>
        {
            throw new NotImplementedException();
        }

        [MustUseReturnValue]
        public static bool ComputeHash(
            [DisallowNull] this UIDefinition definition,
            out Hash128 hash,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }
}
