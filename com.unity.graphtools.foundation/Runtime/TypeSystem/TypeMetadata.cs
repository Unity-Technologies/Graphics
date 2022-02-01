using System;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Information about a <see cref="TypeHandle"/>.
    /// </summary>
    public class TypeMetadata : ITypeMetadata
    {
        readonly Type m_Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMetadata" /> class.
        /// </summary>
        /// <param name="typeHandle">The <see cref="TypeHandle"/> instance represented by the metadata.</param>
        /// <param name="type">The type represented by <paramref name="typeHandle"/>.</param>
        public TypeMetadata(TypeHandle typeHandle, Type type)
        {
            TypeHandle = typeHandle;
            m_Type = type;
        }

        /// <inheritdoc />
        public TypeHandle TypeHandle { get; }

        /// <inheritdoc />
        public string FriendlyName => TypeHandle.IsCustomTypeHandle ? TypeHandle.Identification : m_Type.FriendlyName();

        /// <inheritdoc />
        public string Namespace => m_Type.Namespace ?? string.Empty;

        /// <inheritdoc />
        public bool IsEnum => m_Type.IsEnum;

        /// <inheritdoc />
        public bool IsClass => m_Type.IsClass;

        /// <inheritdoc />
        public bool IsValueType => m_Type.IsValueType;

        /// <inheritdoc />
        public bool IsAssignableFrom(ITypeMetadata metadata) => metadata.IsAssignableTo(m_Type);

        /// <inheritdoc />
        public bool IsAssignableFrom(Type type) => m_Type.IsAssignableFrom(type);

        /// <inheritdoc />
        public bool IsAssignableTo(Type t) => t.IsAssignableFrom(m_Type);

        /// <inheritdoc />
        public bool IsSubclassOf(Type t) => m_Type.IsSubclassOf(t);

        /// <inheritdoc />
        public bool IsSuperclassOf(Type t) => t.IsSubclassOf(m_Type);
    }
}
