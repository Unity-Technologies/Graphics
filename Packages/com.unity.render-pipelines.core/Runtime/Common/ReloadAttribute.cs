using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Attribute specifying information to reload with <see cref="ResourceReloader"/>. This is only
    /// used in the editor and doesn't have any effect at runtime.
    /// </summary>
    /// <seealso cref="ResourceReloader"/>
    /// <seealso cref="ReloadGroupAttribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReloadAttribute : Attribute
    {
        /// <summary>
        /// Lookup method for a resource.
        /// </summary>
        public enum Package
        {
            /// <summary>
            /// Used for builtin resources when the resource isn't part of the package (i.e. builtin
            /// shaders).
            /// </summary>
            Builtin,

            /// <summary>
            /// Used for resources inside the package.
            /// </summary>
            Root,

            /// <summary>
            /// Used for builtin extra resources when the resource isn't part of the package (i.e. builtin
            /// extra Sprite).
            /// </summary>
            BuiltinExtra,
        };

#if UNITY_EDITOR
        /// <summary>
        /// The lookup method.
        /// </summary>
        public readonly Package package;

        /// <summary>
        /// Search paths.
        /// </summary>
        public readonly string[] paths;
#endif

        /// <summary>
        /// Creates a new <see cref="ReloadAttribute"/> for an array by specifying each resource
        /// path individually.
        /// </summary>
        /// <param name="paths">Search paths</param>
        /// <param name="package">The lookup method</param>
        public ReloadAttribute(string[] paths, Package package = Package.Root)
        {
#if UNITY_EDITOR
            this.paths = paths;
            this.package = package;
#endif
        }

        /// <summary>
        /// Creates a new <see cref="ReloadAttribute"/> for a single resource.
        /// </summary>
        /// <param name="path">Search path</param>
        /// <param name="package">The lookup method</param>
        public ReloadAttribute(string path, Package package = Package.Root)
            : this(new[] { path }, package)
        { }

        /// <summary>
        /// Creates a new <see cref="ReloadAttribute"/> for an array using automatic path name
        /// numbering.
        /// </summary>
        /// <param name="pathFormat">The format used for the path</param>
        /// <param name="rangeMin">The array start index (inclusive)</param>
        /// <param name="rangeMax">The array end index (exclusive)</param>
        /// <param name="package">The lookup method</param>
        public ReloadAttribute(string pathFormat, int rangeMin, int rangeMax,
            Package package = Package.Root)
        {
#if UNITY_EDITOR
            this.package = package;
            paths = new string[rangeMax - rangeMin];
            for (int index = rangeMin, i = 0; index < rangeMax; ++index, ++i)
                paths[i] = string.Format(pathFormat, index);
#endif
        }
    }
}
