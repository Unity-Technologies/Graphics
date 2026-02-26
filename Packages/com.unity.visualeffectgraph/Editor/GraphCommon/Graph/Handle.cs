using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Defines an interface for objects that have versioning capabilities.
    /// </summary>
    /*public*/ interface IVersioned
    {
        /// <summary>
        /// Gets the current version of the object.
        /// </summary>
        uint Version { get; }
    }

    /// <summary>
    /// Represents a weak reference to a versioned object that can detect if the target object has changed.
    /// </summary>
    /// <typeparam name="T">The type of the referenced object, which must implement <see cref="IVersioned"/>.</typeparam>
    /*public*/ struct Handle<T> where T : class, IVersioned
    {
        T m_Owner;
        uint m_Version;

        /// <summary>
        /// Gets a value indicating whether the weak reference is still valid.
        /// A reference is valid when the owner exists and has the same version as when the reference was created.
        /// </summary>
        public bool Valid => m_Owner != null && m_Owner.Version == m_Version;

        /// <summary>
        /// Gets the referenced object if the reference is valid, or null otherwise.
        /// </summary>
        public T Ref => Valid ? m_Owner : null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Weak{T}"/> struct with the specified owner.
        /// </summary>
        /// <param name="owner">The object to reference.</param>
        public Handle(T owner)
        {
            Debug.Assert(owner != null);
            m_Owner = owner;
            m_Version = owner.Version;
        }

        /// <summary>
        /// Implicitly converts an object to a weak reference to that object.
        /// </summary>
        /// <param name="owner">The object to create a weak reference to.</param>
        /// <returns>A new weak reference to the specified object.</returns>
        public static implicit operator Handle<T>(T owner) => new(owner);

        /// <summary>
        /// Implicitly converts a weak reference to its target object if the reference is valid.
        /// </summary>
        /// <param name="weak">The weak reference to convert.</param>
        /// <returns>The referenced object, or null if the reference is invalid.</returns>
        public static implicit operator T(Handle<T> weak) => weak.Ref;

        /// <summary>
        /// Invalidates the weak reference by setting its version to zero.
        /// </summary>
        public void Invalidate()
        {
            m_Version = 0;
        }

        /// <summary>
        /// Updates the version of the weak reference to match the current version of the referenced object.
        /// </summary>
        public void Update()
        {
            m_Version = m_Owner.Version;
        }
    }
}
