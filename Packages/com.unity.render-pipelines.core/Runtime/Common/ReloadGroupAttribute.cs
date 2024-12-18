using System;
using UnityEditor;
using static UnityEngine.Rendering.DebugUI.Table;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Attribute specifying that fields of this type should be inspected in depth by the <see cref="ResourceReloader"/>. 
    /// If the associated class instance is null, the system attempts to recreate it using its default constructor.
    /// </summary>
    /// <remarks>
    /// Make sure classes using it have a default constructor!
    /// </remarks>
    /// <seealso cref="ResourceReloader"/>
    /// <seealso cref="ReloadAttribute"/>
    /// <example>
    /// <para> This shows how to use the attribute in the expected scenario. This is particularly useful for content creators.
    /// Adding a new field to a class that defines an asset results in null values for existing instances missing the field in their serialized data. Therefore, when a new field is added, a system for reloading null values may be necessary. </para>
    /// <code>
    ///using UnityEngine;
    ///using UnityEditor;
    ///
    ///[ReloadGroup]
    ///public class MyShaders
    ///{
    ///    [Reload("Shaders/Blit.shader")]
    ///    public Shader blit;
    ///}
    ///
    ///public class MyResourcesAsset : ScriptableObject
    ///{
    ///    // Object used for contextualizing would resolve to be null in already existing
    ///    // instance of MyResourcesAsset that already exists.
    ///    public MyShaders shaders;
    ///
    ///    [Reload("Textures/BayerMatrix.png")]
    ///    public Texture2D bayerMatrixTex;
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
    ///        // (e.g.: adding new field in MyResourcesAsset or MyShaders classes)
    ///        ResourceReloader.ReloadAllNullIn(resources, "Packages/com.my-custom-package/");
    ///        return resources;
    ///    }
    ///}
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ReloadGroupAttribute : Attribute
    { }
}
