using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor;

// Note: This is safe for AOT platforms:
//  - If you use the object based API, it will fallback on a in memory dictionary
//  - If you use the generic API, the AOT compiler will pick up the associated classes and include them into the build.
namespace UnityEngine.Rendering
{
    /// <summary>
    ///     Use this attribute to specify that a type is compatible with another.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
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
        static IsSupportedOn()
        {
            if (!SupportedOnAttributeSetter.isLoaded)
                Debug.LogWarning("IsSupportedOn is not loaded.");
        }

        static DynamicTypeRelation s_Relations = new DynamicTypeRelation();

        /// <summary>
        ///     Is <typeparamref name="TSubject" /> supported by  <typeparamref name="TTarget" />?.
        ///     This is 10x faster than querying dynamically.
        ///     See <see cref="IsSupportedOn{TSubject,TTarget}.IsImplicit" />.
        /// </summary>
        /// <typeparam name="TSubject">Which type will support <typeparamref name="TTarget" />.</typeparam>
        /// <typeparam name="TTarget">Defines what to support.</typeparam>
        /// <returns>Is <typeparamref name="TSubject" /> supported by  <typeparamref name="TTarget" />?.</returns>
        public static bool IsSupportedBy<TSubject, TTarget>()
        {
            return IsSupportedOn<TSubject, TTarget>.IsSupported;
        }

        /// <summary>
        ///     Is <typeparamref name="TSubject" /> explicitly supported by  <typeparamref name="TTarget" />?.
        ///     This is 10x faster than querying dynamically.
        ///     See <see cref="IsSupportedOn{TSubject,TTarget}.IsImplicit" />.
        /// </summary>
        /// <typeparam name="TSubject">Which type will support <typeparamref name="TTarget" />.</typeparam>
        /// <typeparam name="TTarget">Defines what to support.</typeparam>
        /// <returns>Is <typeparamref name="TSubject" /> supported by  <typeparamref name="TTarget" />?.</returns>
        public static bool IsExplicitlySupportedBy<TSubject, TTarget>()
        {
            return IsSupportedOn<TSubject, TTarget>.IsExplicit;
        }

        /// <summary>
        ///     Does <typeparamref name="TSubject" /> explicitly support another type?
        /// </summary>
        /// <typeparam name="TSubject">Does it explicitly support another type?</typeparam>
        /// <returns>Does <typeparamref name="TSubject" /> explicitly support another type?</returns>
        public static bool HasExplicitSupport<TSubject>()
        {
            return HasIsSupportedOn<TSubject>.Value;
        }

        /// <summary>
        ///     Set the value in the static types
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target" />.</param>
        /// <param name="target">Defines what to support.</param>
        internal static void RegisterStaticRelation([DisallowNull] Type subject, [DisallowNull] Type target)
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
                .GetProperty(nameof(IsSupportedOn<bool, bool>.IsExplicit), BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, true);
        }

        /// <summary>
        ///     Set the value in a dictionary.
        ///     Note for AOT platforms: it may be required to use the dynamic backend because using generic structs
        ///     require JIT.
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target" />.</param>
        /// <param name="target">Defines what to support.</param>
        internal static void RegisterDynamicRelation([DisallowNull] Type subject, [DisallowNull] Type target)
        {
            s_Relations.RegisterRelation(subject, target);
        }

        /// <summary>
        ///     Is <paramref name="subject" /> supported by  <paramref name="target" />?
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target" />.</param>
        /// <param name="target">Defines what to support.</param>
        /// <returns></returns>
        public static bool IsSupportedBy([DisallowNull] Type subject, [DisallowNull] Type target)
        {
            return !s_Relations.HasRelations(subject) || s_Relations.AreRelated(subject, target);
        }

        /// <summary>
        ///     Is <paramref name="subject" /> explicitly supported by  <paramref name="target" />?
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target" />.</param>
        /// <param name="target">Defines what to support.</param>
        /// <returns></returns>
        public static bool IsExplicitlySupportedBy([DisallowNull] Type subject, [DisallowNull] Type target)
        {
            return s_Relations.AreRelated(subject, target);
        }

        /// <summary>
        ///     Does <paramref name="subject" /> explicitly support another type?
        /// </summary>
        /// <param name="subject">Does it explicitly support another type?</param>
        /// <returns></returns>
        public static bool HasExplicitSupport([DisallowNull] Type subject)
        {
            return s_Relations.HasRelations(subject);
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
    ///     Use <see cref="IsImplicit" /> to know if <typeparamref name="TSubject" /> explicitly supports
    ///     <typeparamref name="TTarget" />.
    /// </summary>
    /// <typeparam name="TSubject">The subject to query.</typeparam>
    /// <typeparam name="TTarget">The type that defines what to support.</typeparam>

    // ReSharper disable once UnusedTypeParameter
    public struct IsSupportedOn<TSubject, TTarget>
    {
        public static bool IsExplicit { get; set; } = false;

        /// <summary>
        ///     Use it to know if <typeparamref name="TSubject" /> implicitly supports <typeparamref name="TTarget" />.
        /// </summary>
        public static bool IsImplicit => !HasIsSupportedOn<TSubject>.Value;

        /// <summary>
        ///     Use it to know if <typeparamref name="TSubject" />
        ///     supports <typeparamref name="TTarget" /> implicitly or explicitly.
        /// </summary>
        public static bool IsSupported => IsExplicit || IsImplicit;
    }

    #region Registration Executor

    /// <summary>
    ///     Execute the registration code of the attribute.
    ///     NOTE: This can be replaced by a code generation at compile time when available.
    /// </summary>
    static class SupportedOnAttributeSetter
    {
        public static bool isLoaded { get; private set; } = false;

        static SupportedOnAttributeSetter()
        {
            // Note: Querying type with attribute with TypeCache is 4x faster that querying for assembly attribute
            foreach (var type in TypeCache.GetTypesWithAttribute<SupportedOnAttribute>())
            {
                foreach (var attribute in type.GetCustomAttributes(typeof(SupportedOnAttribute)).Cast<SupportedOnAttribute>())
                {
                    if (attribute?.target == null)
                        continue;

                    IsSupportedOn.RegisterStaticRelation(type, attribute.target);
                    IsSupportedOn.RegisterDynamicRelation(type, attribute.target);
                }
            }

            isLoaded = true;
        }
    }

    #endregion
}
