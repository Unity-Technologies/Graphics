#define SUPPORT_DYNAMIC

using System;
using System.Reflection;

// Note: This is safe for AOT platforms:
//  - If you use the object based API, it will fallback on a in memory dictionary
//  - If you use the generic API, the AOT compiler will pick up the associated classes and include them into the build.
namespace UnityEngine.Rendering
{
    /// <summary>
    ///     Use this attribute to specify that a type is compatible with another.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportedOnAttribute : Attribute
    {
        /// <summary>
        ///     Constructor of the attribute.
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target" />.</param>
        /// <param name="target">Defines what to support.</param>
        public SupportedOnAttribute(Type target)
        {
            this.target = target;
        }

        public Type target { get; }
    }

    /// <summary>
    ///     Utilities to query the IsSupported relation.
    /// </summary>
    public static class IsSupportedOn
    {
#if SUPPORT_DYNAMIC
        static DynamicTypeRelation s_Relations = new DynamicTypeRelation();
#else
        const string k_Unsupported = "Dynamic querying is unsupported, please add SUPPORT_DYNAMIC keyword";
#endif

        /// <summary>
        ///     Is <typeparamref name="TSubject" /> supported by  <typeparamref name="TTarget" />?.
        ///     This is 10x faster than querying dynamically.
        ///     See <see cref="IsSupportedOn{TSubject,TTarget}.Value" />.
        /// </summary>
        /// <typeparam name="TSubject">Which type will support <typeparamref name="TTarget" />.</typeparam>
        /// <typeparam name="TTarget">Defines what to support.</typeparam>
        /// <returns>Is <typeparamref name="TSubject" /> supported by  <typeparamref name="TTarget" />?.</returns>
        public static bool IsRelated<TSubject, TTarget>()
        {
            return IsSupportedOn<TSubject, TTarget>.Value;
        }

        /// <summary>
        ///     Does <typeparamref name="TSubject" /> explicitly support another type?
        /// </summary>
        /// <typeparam name="TSubject">Does it explicitly support another type?</typeparam>
        /// <returns>Does <typeparamref name="TSubject" /> explicitly support another type?</returns>
        public static bool HasRelations<TSubject>()
        {
            return HasIsSupportedOn<TSubject>.Value;
        }

        /// <summary>
        ///     Set the value in the static types
        ///
        ///     Note: This API is public because it is used by Source Generators to statically register the types.
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target" />.</param>
        /// <param name="target">Defines what to support.</param>
        public static void RegisterStaticRelation(Type subject, Type target)
        {
            // Note: Null Ref for types.
            //   We fetch an API that is defined below, we fully own the type definition.
            //   So there must be no situation where the properties are not found.

            var hasIsSupportedOn = typeof(HasIsSupportedOn<>).MakeGenericType(subject);

            // ReSharper disable once PossibleNullReferenceException
            hasIsSupportedOn
                .GetProperty(nameof(HasIsSupportedOn<bool>.Value), BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, true);

            var isSupportedOn = typeof(IsSupportedOn<,>).MakeGenericType(subject, target);

            // ReSharper disable once PossibleNullReferenceException
            isSupportedOn
                .GetProperty(nameof(IsSupportedOn<bool, bool>.internalValue), BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, true);
        }

        /// <summary>
        ///     Set the value in a dictionary.
        ///
        ///     Note: This API is public because it is used by Source Generators to statically register the types.
        ///     Note for AOT platforms: it may be required to use the dynamic backend because using generic structs
        ///     require JIT.
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target" />.</param>
        /// <param name="target">Defines what to support.</param>
        public static void RegisterDynamicRelation(Type subject, Type target)
        {
#if SUPPORT_DYNAMIC
            s_Relations.RegisterRelation(subject, target);
#endif
        }

        /// <summary>
        ///     Is <paramref name="subject" /> supported by  <paramref name="target" />?
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target" />.</param>
        /// <param name="target">Defines what to support.</param>
        /// <returns></returns>
        public static bool IsRelated(Type subject, Type target)
        {
#if SUPPORT_DYNAMIC
            return s_Relations.IsRelated(subject, target);
#else
            Debug.LogWarning(k_Unsupported);
            return default;
#endif
        }

        /// <summary>
        ///     Does <paramref name="subject" /> explicitly support another type?
        /// </summary>
        /// <param name="subject">Does it explicitly support another type?</param>
        /// <returns></returns>
        public static bool HasRelations(Type subject)
        {
#if SUPPORT_DYNAMIC
            return s_Relations.HasRelations(subject);
#else
            Debug.LogWarning(k_Unsupported);
            return default;
#endif
        }
    }

    /// <summary>
    ///     Use <see cref="Value" /> to know if <typeparamref name="TSubject" /> explicitly support at least one type.
    /// </summary>
    /// <typeparam name="TSubject">The type to query.</typeparam>

    // ReSharper disable once UnusedTypeParameter
    public struct HasIsSupportedOn<TSubject>
    {
        /// <summary>
        ///     Use it to know if
        ///     <typeparam name="TSubject" />
        ///     explicitly support at least one type.
        /// </summary>

        // ReSharper disable once StaticMemberInGenericType
        public static bool Value { get; set; } = false;
    }

    /// <summary>
    ///     Use <see cref="Value" /> to know if <typeparamref name="TSubject" /> explicitly supports
    ///     <typeparamref name="TTarget" />.
    /// </summary>
    /// <typeparam name="TSubject">The subject to query.</typeparam>
    /// <typeparam name="TTarget">The type that defines what to support.</typeparam>

    // ReSharper disable once UnusedTypeParameter
    public struct IsSupportedOn<TSubject, TTarget>
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static bool internalValue { get; set; } = false;

        /// <summary>
        ///     Use it to know if <typeparamref name="TSubject" /> explicitly supports <typeparamref name="TTarget" />.
        /// </summary>
        public static bool Value
            => HasIsSupportedOn<TSubject>.Value && internalValue || !HasIsSupportedOn<TSubject>.Value;
    }
}
