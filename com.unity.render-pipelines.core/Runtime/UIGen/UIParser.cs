using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    public class TypeToUIDefinitionParser
    {
        /// <summary>
        /// Find all property tagged with <see cref="DebugMenuPropertyAttribute"/>.
        ///
        /// Search recursively all types used as field or member that are not tagged with <see cref="DebugMenuPropertyAttribute"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="definition"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        [MustUseReturnValue]
        public static bool ParseTypeRecursive(
            [DisallowNull] Type type,
            [NotNullWhen(true)] out UIDefinition definition,
            [NotNullWhen(false)] out Exception error
        )
        {
            // parse type to find all declared properties
            // parse type to find all used types
            // parse all used type (except the one used to declare properties, they are leaves in the tree)
            // then merge all definitions
            throw new NotImplementedException();
        }

        [MustUseReturnValue]
        public static bool ParseType(
            [DisallowNull] Type type,
            [NotNullWhen(true)] out UIDefinition definition,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }

}
