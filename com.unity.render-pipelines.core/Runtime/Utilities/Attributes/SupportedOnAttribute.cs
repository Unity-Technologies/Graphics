using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

// TODO: Evaluate performance improvement at domain reload, vs all type attribute parsing.

// Note: Static method is about 10x faster at querying for generic API than dynamic method
// Note: This is safe for AOT platforms:
//  - If you use the object based API, it will fallback on a in memory dictionary
//  - If you use the generic API, the AOT compiler will pick up the associated classes and include them into the build.
namespace UnityEngine.Rendering
{
    #region Public API
    /// <summary>
    /// Use this attribute to specify that a type is compatible with another.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class SupportedOnAttribute : Attribute
    {
        /// <summary>
        /// Constructor of the attribute.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        public SupportedOnAttribute(Type subject, Type target)
        {
            IsSupportedOn.RegisterStaticRelation(subject, target);
            IsSupportedOn.RegisterDynamicRelation(subject, target);
        }
    }

    /// <summary>
    /// Utilities to query the IsSupported relation.
    /// </summary>
    public static partial class IsSupportedOn
    {
        /// <summary>
        /// Is <typeparamref name="TSubject"/> supported by  <typeparamref name="TTarget"/>?
        /// </summary>
        /// <typeparam name="TSubject"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <returns></returns>
        public static bool IsRelated<TSubject, TTarget>()
        {
            return StaticIsRelated<TSubject, TTarget>();
        }

        /// <summary>
        /// Is <paramref name="subject"/> supported by  <typeparamref name="target"/>?
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsRelated(Type subject, Type target)
        {
            return DynamicIsRelated(subject, target);
        }

        /// <summary>
        /// Does <typeparamref name="TSubject"/> explicitly support another type?
        /// </summary>
        /// <typeparam name="TSubject"></typeparam>
        /// <returns></returns>
        public static bool HasRelations<TSubject>()
        {
            return StaticHasRelations<TSubject>();
        }

        /// <summary>
        /// Does <paramref name="subject"/> explicitly support another type?
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        public static bool HasRelations(Type subject)
        {
            return DynamicHasRelations(subject);
        }
    }

    /// <summary>
    /// Use <see cref="Value"/> to know if <typeparamref name="TSubject"/> explicitly support at least one type.
    /// </summary>
    /// <typeparam name="TSubject"></typeparam>
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
    /// <typeparam name="TSubject"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
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
    #endregion

    #region Static Query support
    public static partial class IsSupportedOn
    {
        /// <summary>
        /// Set the value in the static types
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        internal static void RegisterStaticRelation(Type subject, Type target)
        {
            var hasIsSupportedOn = typeof(HasIsSupportedOn<>).MakeGenericType(subject);
            hasIsSupportedOn.GetProperty("Value", BindingFlags.Static | BindingFlags.Public).SetValue(null, true);

            var isSupportedOn = typeof(IsSupportedOn<,>).MakeGenericType(subject, target);
            isSupportedOn.GetProperty("InternalValue", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
        }

        internal static bool StaticIsRelated<TSubject, TTarget>() => IsSupportedOn<TSubject, TTarget>.Value;

        internal static bool StaticIsRelated(Type subject, Type target)
        {
            if (subject == null || target == null)
                return false;

            return (bool)typeof(IsSupportedOn<,>).MakeGenericType(subject,
                    target)
                .GetProperty("Value",
                    BindingFlags.Static | BindingFlags.Public)
                .GetValue(null);
        }

        internal static bool StaticHasRelations<TSubject>() => HasIsSupportedOn<TSubject>.Value;

        internal static bool StaticHasRelations(Type subject)
        {
            if (subject == null)
                return false;

            return (bool)typeof(HasIsSupportedOn<>).MakeGenericType(subject)
                .GetProperty("Value",
                    BindingFlags.Static | BindingFlags.Public)
                .GetValue(null);
        }
    }
    #endregion

    #region Dynamic Query Support
    public static partial class IsSupportedOn
    {
        static DynamicTypeRelation s_Relations = new DynamicTypeRelation();

        /// <summary>
        /// Set the value in a dictionary.
        ///
        /// Note for AOT platforms: it may be required to use the dynamic backend because using generic structs
        ///   require JIT.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="target"></param>
        internal static void RegisterDynamicRelation(Type subject, Type target)
        {
            s_Relations.RegisterRelation(subject, target);
        }

        internal static bool DynamicIsRelated<TSubject, TTarget>() => DynamicIsRelated(typeof(TSubject), typeof(TTarget));

        internal static bool DynamicIsRelated(Type subject, Type target)
        {
            return s_Relations.IsRelated(subject, target);
        }

        internal static bool DynamicHasRelations<TSubject>() => DynamicHasRelations(typeof(TSubject));

        internal static bool DynamicHasRelations(Type subject)
        {
            return s_Relations.HasRelations(subject);
        }
    }
    #endregion

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

            // Make sure to run the attribute constructor once
            // The registration happens in the attribute constructor: you don't need to store anything in the attribute.
            s_AssemblyConstructorRan = true;
            s_AssembliesWithAttribute = AppDomain.CurrentDomain.GetAssemblies()
                .Count(assembly => assembly.GetCustomAttributes(typeof(SupportedOnAttribute), true).Length > 0);
        }

        static bool s_AssemblyConstructorRan = false;
        static int s_AssembliesWithAttribute = 0;
    }
    #endregion
}
