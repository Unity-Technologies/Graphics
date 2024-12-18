using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// The <see cref="ReloadAttribute"/> attribute specifies paths for loading or reloading resources and has no direct action.
    /// Used with the <see cref="ResourceReloader"/> to define where to load data for null fields.
    /// </summary>
    /// <remarks>
    /// This attribute is designed for use in the Unity Editor and has no effect at runtime.
    /// 
    /// <see cref="IRenderPipelineResources"/> have their own attribute <see cref="ResourcePathAttribute"/> to do this.
    /// When using them, resource reloading is handled automatically by the engine and does not require calling ResourceReloader.
    /// 
    /// While ResourceReloader was originally created for handling Scriptable Render Pipeline (SRP) resources, it has been replaced by <see cref="IRenderPipelineResources"/>.
    /// The <see cref="ResourceReloader"/>, <see cref="ResourceReloader"/> and <see cref="ReloadGroupAttribute"/> remain available for for user-defined assets.
    /// </remarks>
    /// <seealso cref="ResourceReloader"/>
    /// <seealso cref="ReloadGroupAttribute"/>
    /// <example>
    /// <para> This shows how to use the attribute in the expected scenario. This is particularly useful for content creators.
    /// Adding a new field to a class that defines an asset results in null values for existing instances missing the field in their serialized data. Therefore, when a new field is added, a system for reloading null values may be necessary. </para>
    /// <code>
    ///using UnityEngine;
    ///using UnityEditor;
    ///
    ///public class MyResourcesAsset : ScriptableObject
    ///{
    ///    [Reload("Shaders/Blit.shader")]
    ///    public Shader blit;
    ///    
    ///    // Added in version 2
    ///    [Reload("Shaders/betterBlit.shader")]
    ///    public Shader betterBlit;
    ///}
    ///
    ///public static class MyResourceHandler
    ///{
    ///    public static MyResourcesAsset GetAndReload()
    ///    {
    ///        var resources = AssetDatabase.LoadAssetAtPath&lt;MyResourcesAsset&gt;("MyResources.asset");
    ///
    ///        // Ensure that update of the data layout of MyResourcesAsset
    ///        // will not result in null value for asset already existing.
    ///        // (e.g.: added betterBlit in the case above)
    ///        ResourceReloader.ReloadAllNullIn(resources, "Packages/com.my-custom-package/");
    ///        return resources;
    ///    }
    ///}
    /// </code>
    /// </example>
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
        /// <example>
        /// <para> This example demonstrates how to handle arrays with different resource paths. </para>
        /// <code>
        ///using UnityEngine;
        ///
        ///public class MyResourcesAsset : ScriptableObject
        ///{
        ///    [ResourcePaths(new[]
        ///    {
        ///        "Texture/FilmGrain/Thin.png",
        ///        "Texture/FilmGrain/Medium.png",
        ///        "Texture/FilmGrain/Large.png",
        ///    })]
        ///    public Texture[] filmGrains;
        ///}
        /// </code>
        /// </example>
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
        /// <example>
        /// <para> This example shows how to directly specify the path of an asset. </para>
        /// <code>
        ///using UnityEngine;
        ///
        ///public class MyResourcesAsset : ScriptableObject
        ///{
        ///    [Reload("Shaders/Blit.shader")]
        ///    public Shader blit;
        ///}
        /// </code>
        /// </example>
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
        /// <example>
        /// <para> This example demonstrates handling arrays with resource paths that share a common format, differing only by an index. </para>
        /// <code>
        ///using UnityEngine;
        ///
        ///public class MyResourcesAsset : ScriptableObject
        ///{
        ///    // The following will seek for resources:
        ///    //  - Texture/FilmGrain/Thin1.png
        ///    //  - Texture/FilmGrain/Thin2.png
        ///    //  - Texture/FilmGrain/Thin3.png
        ///    [ResourcePaths("Texture/FilmGrain/Thin{0}.png", 1, 4)]
        ///    public Texture[] thinGrains;
        ///}
        /// </code>
        /// </example>
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
