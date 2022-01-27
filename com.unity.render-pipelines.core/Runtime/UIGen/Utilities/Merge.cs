using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public interface ITakeAndMerge<T>
    {
        [MustUseReturnValue]
        bool TakeAndMerge(
            [DisallowNull] T input,
            [NotNullWhen(false)] out Exception error
        );
    }

    public static class MergeExtensions
    {
        [MustUseReturnValue]
        public static bool AggregateInto<TList, TValue>(
            [DisallowNull] this TList values,
            [DisallowNull] TValue merged,
            [NotNullWhen(false)] out Exception error
        )
            where TList : IList<TValue>
            where TValue : ITakeAndMerge<TValue>
        {
            foreach (var value in values)
            {
                if (!merged.TakeAndMerge(value, out error))
                    return false;
            }

            error = default;
            return true;
        }
    }
}
