#define SUPPORT_DYNAMIC

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

// Note: This is safe for AOT platforms:
//  - If you use the object based API, it will fallback on a in memory dictionary
//  - If you use the generic API, the AOT compiler will pick up the associated classes and include them into the build.
namespace UnityEngine.Rendering
{
    /// <summary>
    /// Use this attribute to specify that a type is compatible with another.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SupportedOnAttribute : Attribute
    {
        /// <summary>
        /// Constructor of the attribute.
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target"/>.</param>
        /// <param name="target">Defines what to support.</param>
        public SupportedOnAttribute(Type subject, Type target)
        {
            // Call directly registration here. This avoids storing properties in the memory footprint
            IsSupportedOn.RegisterStaticRelation(subject, target);
            IsSupportedOn.RegisterDynamicRelation(subject, target);
        }
    }

    /// <summary>
    /// Utilities to query the IsSupported relation.
    /// </summary>
    public static class IsSupportedOn
    {
#if SUPPORT_DYNAMIC
        static DynamicTypeRelation s_Relations = new DynamicTypeRelation();
#else
        const string k_Unsupported = "Dynamic querying is unsupported, please add SUPPORT_DYNAMIC keyword";
#endif

        /// <summary>
        /// Is <typeparamref name="TSubject"/> supported by  <typeparamref name="TTarget"/>?.
        ///
        /// This is 10x faster than querying dynamically.
        /// See <see cref="IsSupportedOn{TSubject,TTarget}.Value"/>.
        /// </summary>
        /// <typeparam name="TSubject">Which type will support <typeparamref name="TTarget"/>.</typeparam>
        /// <typeparam name="TTarget">Defines what to support.</typeparam>
        /// <returns>Is <typeparamref name="TSubject"/> supported by  <typeparamref name="TTarget"/>?.</returns>
        public static bool IsRelated<TSubject, TTarget>() => IsSupportedOn<TSubject, TTarget>.Value;

        /// <summary>
        /// Does <typeparamref name="TSubject"/> explicitly support another type?
        /// </summary>
        /// <typeparam name="TSubject">Does it explicitly support another type?</typeparam>
        /// <returns>Does <typeparamref name="TSubject"/> explicitly support another type?</returns>
        public static bool HasRelations<TSubject>() => HasIsSupportedOn<TSubject>.Value;

        /// <summary>
        /// Set the value in the static types
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target"/>.</param>
        /// <param name="target">Defines what to support.</param>
        internal static void RegisterStaticRelation(Type subject, Type target)
        {
            var hasIsSupportedOn = typeof(HasIsSupportedOn<>).MakeGenericType(subject);
            hasIsSupportedOn.GetProperty("Value", BindingFlags.Static | BindingFlags.Public).SetValue(null, true);

            var isSupportedOn = typeof(IsSupportedOn<,>).MakeGenericType(subject, target);
            isSupportedOn.GetProperty("InternalValue", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
        }

        /// <summary>
        /// Set the value in a dictionary.
        ///
        /// Note for AOT platforms: it may be required to use the dynamic backend because using generic structs
        ///   require JIT.
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target"/>.</param>
        /// <param name="target">Defines what to support.</param>
        [Conditional("SUPPORT_DYNAMIC")]
        internal static void RegisterDynamicRelation(Type subject, Type target)
        {
#if SUPPORT_DYNAMIC
            s_Relations.RegisterRelation(subject, target);
#endif
        }

        /// <summary>
        /// Is <paramref name="subject"/> supported by  <typeparamref name="target"/>?
        /// </summary>
        /// <param name="subject">Which type will support <paramref name="target"/>.</param>
        /// <param name="target">Defines what to support.</param>
        /// <returns></returns>
        public static bool IsRelated(Type subject, Type target)
        {
#if SUPPORT_DYNAMIC
            return s_Relations.IsRelated(subject, target);
#else
            Debug.Log(k_Unsupported);
            return default;
#endif
        }

        /// <summary>
        /// Does <paramref name="subject"/> explicitly support another type?
        /// </summary>
        /// <param name="subject">Does it explicitly support another type?</param>
        /// <returns></returns>
        public static bool HasRelations(Type subject)
        {
#if SUPPORT_DYNAMIC
            return s_Relations.HasRelations(subject);
#else
            Debug.Log(k_Unsupported);
            return default;
#endif
        }
    }

    /// <summary>
    /// Use <see cref="Value"/> to know if <typeparamref name="TSubject"/> explicitly support at least one type.
    /// </summary>
    /// <typeparam name="TSubject">The type to query.</typeparam>
    public struct HasIsSupportedOn<TSubject>
    {
        /// <summary>
        /// Use it to know if <typeparam name="TSubject"/> explicitly support at least one type.
        /// </summary>
        public static bool Value { get; private set; } = false;
    }

    /// <summary>
    /// Use <see cref="Value"/> to know if <typeparamref name="TSubject"/> explicitly supports <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TSubject">The subject to query.</typeparam>
    /// <typeparam name="TTarget">The type that defines what to support.</typeparam>
    public struct IsSupportedOn<TSubject, TTarget>
    {
        static bool InternalValue
        { get; set; } = false;

        /// <summary>
        /// Use it to know if <typeparamref name="TSubject"/> explicitly supports <typeparamref name="TTarget"/>.
        /// </summary>
        public static bool Value
            => HasIsSupportedOn<TSubject>.Value && InternalValue || !HasIsSupportedOn<TSubject>.Value;
    }

    #region Registration Executor
    /// <summary>
    /// Execute the registration code of the attribute.
    ///
    /// NOTE: This can be replaced by a code generation at compile time when available.
    /// </summary>
    static class SupportedOnAttributeSetter
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            if (s_AssemblyConstructorRan)
                return;

            // Note: Querying type with attribute with TypeCache is 4x faster that querying for assembly attribute
            foreach (var type in UnityEditor.TypeCache.GetTypesWithAttribute<SupportedOnAttribute>())
            {
                // Trigger attribute constructor here
                var _ = type.GetCustomAttributes(typeof(SupportedOnAttribute)).FirstOrDefault() as SupportedOnAttribute;
            }
        }

        static bool s_AssemblyConstructorRan = false;
    }
    #endregion
}
