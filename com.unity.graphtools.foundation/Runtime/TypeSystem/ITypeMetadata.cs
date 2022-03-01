using System;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Information about a <see cref="TypeHandle"/>.
    /// </summary>
    public interface ITypeMetadata
    {
        /// <summary>
        /// The <see cref="TypeHandle"/> referenced by this metadata.
        /// </summary>
        TypeHandle TypeHandle { get; }

        /// <summary>
        /// A human readable name of the type.
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// The namespace of the type.
        /// </summary>
        string Namespace { get; }

        /// <summary>
        /// Whether this type is an enum type.
        /// </summary>
        bool IsEnum { get; }

        /// <summary>
        /// Whether this type is a class.
        /// </summary>
        bool IsClass { get; }

        /// <summary>
        /// Whether this type is a value type.
        /// </summary>
        bool IsValueType { get; }

        /// <summary>
        /// Determines whether an instance of this type is assignable from an instance of <paramref name="metadata"/>.
        /// </summary>
        /// <param name="metadata">The other type.</param>
        /// <returns>True if an instance of this type is assignable from an instance of <paramref name="metadata"/>.</returns>
        bool IsAssignableFrom(ITypeMetadata metadata);

        /// <summary>
        /// Determines whether this an instance of this type is assignable from an instance of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The other type.</param>
        /// <returns>True if an instance of this type is assignable from an instance of <paramref name="type"/>.</returns>
        bool IsAssignableFrom(Type type);

        /// <summary>
        /// Determines whether an instance of this type is assignable to an instance of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The other type.</param>
        /// <returns>True if an instance of this type is assignable to an instance of <paramref name="type"/>.</returns>
        bool IsAssignableTo(Type type);

        /// <summary>
        /// Determines whether this type is a superclass of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The other type.</param>
        /// <returns>True if this type is a superclass of <paramref name="type"/>.</returns>
        bool IsSuperclassOf(Type type);

        /// <summary>
        /// Determines whether this type is a subclass of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The other type.</param>
        /// <returns>True if this type is a subclass of <paramref name="type"/>.</returns>
        bool IsSubclassOf(Type type);
    }
}
