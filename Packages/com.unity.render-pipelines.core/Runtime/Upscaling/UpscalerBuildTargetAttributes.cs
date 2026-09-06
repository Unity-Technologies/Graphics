#if ENABLE_UPSCALER_FRAMEWORK
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Declares the build targets an <see cref="IUpscaler"/> supports, and optionally the graphics APIs it supports on
    /// them. An upscaler that declares neither this nor <see cref="UpscalerUnsupportedBuildTargetAttribute"/> supports
    /// every build target and graphics API.
    /// </summary>
    /// <remarks>
    /// Read from the upscaler's type, so supported platforms can be listed without instantiating it. This describes
    /// where an upscaler may be used and is not consulted at runtime, where the upscaler validates the device itself.
    /// Apply it once per set of build targets that share the same graphics APIs. Listing build targets excludes
    /// targets added later, so an upscaler that is portable apart from known gaps should declare
    /// <see cref="UpscalerUnsupportedBuildTargetAttribute"/> instead. Declare one or the other, never both: this
    /// attribute already excludes everything it doesn't list, so an upscaler that declares both logs an error at
    /// registration and its unsupported declarations are ignored.
    ///
    /// This is editor-only metadata, but it lives in the runtime assembly because upscalers declare it on themselves
    /// and a runtime assembly cannot reference an editor one.
    /// </remarks>
    /// <example>
    /// <para> The examples below restrict an upscaler to one build target and a subset of that target's graphics APIs,
    /// then declare an upscaler whose supported APIs differ from one build target to the next. </para>
    /// <code>
    ///using UnityEditor;
    ///using UnityEngine.Rendering;
    ///
    ///// Windows only, and only on the APIs the vendor SDK supports there.
    ///[UpscalerSupportedBuildTarget(BuildTarget.StandaloneWindows64,
    ///    graphicsDeviceTypes = new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Vulkan })]
    ///public class MyUpscaler : AbstractUpscaler
    ///{
    ///}
    ///
    ///// A different set of graphics APIs per build target needs one declaration each.
    ///[UpscalerSupportedBuildTarget(BuildTarget.StandaloneWindows64,
    ///    graphicsDeviceTypes = new[] { GraphicsDeviceType.Direct3D12 })]
    ///[UpscalerSupportedBuildTarget(BuildTarget.StandaloneLinux64,
    ///    graphicsDeviceTypes = new[] { GraphicsDeviceType.Vulkan })]
    ///public class MyPortableUpscaler : AbstractUpscaler
    ///{
    ///}
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class UpscalerSupportedBuildTargetAttribute : Attribute
    {
        // Memoizes the attribute reads. Attributes can't change without a domain reload and it is editor only read on the main thread, so it needs no invalidation or locking.
        static readonly Dictionary<Type, (UpscalerSupportedBuildTargetAttribute[] supported, UpscalerUnsupportedBuildTargetAttribute[] unsupported)> s_Declarations = new();

        /// <summary>
        /// The <see cref="BuildTarget"/>s the upscaler supports. Leave unset to support any target.
        /// </summary>
        public BuildTarget[] buildTargets { get; }

        /// <summary>
        /// The <see cref="GraphicsDeviceType"/>s the upscaler supports on the specified <paramref name="buildTargets"/>. Leave unset to
        /// support any API.
        /// </summary>
        public GraphicsDeviceType[] graphicsDeviceTypes { get; set; }  // Assigned as a named property

        /// <summary>
        /// Declares the <see cref="BuildTarget"/>s the upscaler supports. Set the <paramref name="graphicsDeviceTypes"/> property to narrow the
        /// declaration to particular <see cref="GraphicsDeviceType"/>s on those targets.
        /// </summary>
        /// <param name="buildTargets">The <see cref="BuildTarget"/>s the upscaler supports. Leave unset to support any target.</param>
        public UpscalerSupportedBuildTargetAttribute(params BuildTarget[] buildTargets)
        {
            this.buildTargets = buildTargets ?? Array.Empty<BuildTarget>();
        }

        /// <summary>
        /// Returns whether an upscaler supports a build target.
        /// </summary>
        /// <param name="upscalerType">The upscaler type to read the declarations from.</param>
        /// <param name="buildTarget">The build target to check.</param>
        public static bool IsBuildTargetSupported(Type upscalerType, BuildTarget buildTarget)
        {
            var declarations = GetDeclarations(upscalerType);

            // Declaring supported targets excludes every target that isn't declared.
            if (declarations.supported.Length > 0)
            {
                foreach (UpscalerSupportedBuildTargetAttribute attribute in declarations.supported)
                {
                    // A declaration that names no build target applies to all of them.
                    if (attribute.buildTargets.Length == 0 || Array.IndexOf(attribute.buildTargets, buildTarget) >= 0)
                        return true;
                }

                return false;
            }

            // Only reached when nothing is declared as supported, so these declarations narrow an implicit everything.
            foreach (UpscalerUnsupportedBuildTargetAttribute attribute in declarations.unsupported)
            {
                // Naming graphics APIs rules out those APIs rather than the whole target.
                if (attribute.graphicsDeviceTypes != null)
                    continue;

                if (attribute.buildTargets.Length == 0 || Array.IndexOf(attribute.buildTargets, buildTarget) >= 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns whether an upscaler supports a graphics API on a build target. Returns false when the build target
        /// itself is unsupported.
        /// </summary>
        /// <param name="upscalerType">The upscaler type to read the declarations from.</param>
        /// <param name="buildTarget">The build target to check.</param>
        /// <param name="graphicsDeviceType">The graphics API to check.</param>
        public static bool IsGraphicsAPISupported(Type upscalerType, BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType)
        {
            var declarations = GetDeclarations(upscalerType);

            // Declaring supported targets excludes every target that isn't declared, so any unsupported declaration
            // alongside them has nothing left to rule out.
            if (declarations.supported.Length > 0)
            {
                foreach (UpscalerSupportedBuildTargetAttribute attribute in declarations.supported)
                {
                    // A declaration that names no build target applies to all of them.
                    if (attribute.buildTargets.Length > 0 && Array.IndexOf(attribute.buildTargets, buildTarget) < 0)
                        continue;

                    // A declaration that names no API supports every API on its targets.
                    if (attribute.graphicsDeviceTypes == null || Array.IndexOf(attribute.graphicsDeviceTypes, graphicsDeviceType) >= 0)
                        return true;
                }

                return false;
            }

            // Only reached when nothing is declared as supported, so these declarations narrow an implicit everything
            // and need no count check of their own.
            foreach (UpscalerUnsupportedBuildTargetAttribute attribute in declarations.unsupported)
            {
                // A declaration that names no build target applies to all of them.
                if (attribute.buildTargets.Length > 0 && Array.IndexOf(attribute.buildTargets, buildTarget) < 0)
                    continue;

                // Naming no API rules out the whole target, so nothing on it is supported either.
                if (attribute.graphicsDeviceTypes == null || Array.IndexOf(attribute.graphicsDeviceTypes, graphicsDeviceType) >= 0)
                    return false;
            }

            return true;
        }

        // Reflects on first use for an upscaler, then serves every later query from the memo.
        static (UpscalerSupportedBuildTargetAttribute[] supported, UpscalerUnsupportedBuildTargetAttribute[] unsupported) GetDeclarations(Type upscalerType)
        {
            if (!s_Declarations.TryGetValue(upscalerType, out var declarations))
            {
                declarations = (
                    (UpscalerSupportedBuildTargetAttribute[])GetCustomAttributes(upscalerType, typeof(UpscalerSupportedBuildTargetAttribute)),
                    (UpscalerUnsupportedBuildTargetAttribute[])GetCustomAttributes(upscalerType, typeof(UpscalerUnsupportedBuildTargetAttribute)));

                s_Declarations[upscalerType] = declarations;
            }

            return declarations;
        }

        // Reported once per upscaler at registration.
        internal static bool ValidateDeclarations(Type upscalerType)
        {
            var declarations = GetDeclarations(upscalerType);

            // The two describe the same thing from opposite ends, so declaring both discards one of them.
            if (declarations.supported.Length == 0 || declarations.unsupported.Length == 0)
                return true;

            Debug.LogError($"{upscalerType.Name} declares both [SupportedBuildTarget] and [UnsupportedBuildTarget] attributes. Only the [SupportedBuildTarget] declarations will be used.");
            return false;
        }
    }

    /// <summary>
    /// Declares the build targets an <see cref="IUpscaler"/> does not support, and optionally the graphics APIs it
    /// does not support on them. Ignored when the upscaler also declares
    /// <see cref="UpscalerSupportedBuildTargetAttribute"/>, since listing supported targets already excludes the rest.
    /// </summary>
    /// <remarks>
    /// Read from the upscaler's type, so supported platforms can be listed without instantiating it. This describes
    /// where an upscaler may be used and is not consulted at runtime, where the upscaler validates the device itself.
    /// Apply it once per set of build targets that share the same graphics APIs. Use it for an upscaler that is
    /// portable apart from known gaps, so targets added later are supported by default. Declare one or the other,
    /// never both: an upscaler that declares both logs an error at registration and these declarations are ignored.
    ///
    /// This is editor-only metadata, but it lives in the runtime assembly because upscalers declare it on themselves
    /// and a runtime assembly cannot reference an editor one.
    /// </remarks>
    /// <example>
    /// <para> The examples below rule out a build target entirely, then rule out a graphics API wherever it is
    /// available. </para>
    /// <code>
    ///using UnityEditor;
    ///using UnityEngine.Rendering;
    ///
    ///// Portable apart from a platform it cannot run on at all.
    ///[UpscalerUnsupportedBuildTarget(BuildTarget.WebGL)]
    ///public class MyUpscaler : AbstractUpscaler
    ///{
    ///}
    ///
    ///// Portable apart from a graphics API, wherever that API is available.
    ///[UpscalerUnsupportedBuildTarget(graphicsDeviceTypes = new[] { GraphicsDeviceType.OpenGLES3 })]
    ///public class MyComputeUpscaler : AbstractUpscaler
    ///{
    ///}
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class UpscalerUnsupportedBuildTargetAttribute : Attribute
    {
        /// <summary>
        /// The <see cref="BuildTarget"/>s the upscaler does not support. Leave unset to apply to any target.
        /// </summary>
        public BuildTarget[] buildTargets { get; }

        /// <summary>
        /// The <see cref="GraphicsDeviceType"/>s the upscaler does not support on the specified <paramref name="buildTargets"/>. Leave
        /// unset to rule out the build targets entirely rather than particular APIs on them.
        /// </summary>
        public GraphicsDeviceType[] graphicsDeviceTypes { get; set; }  // Assigned as a named property

        /// <summary>
        /// Declares the <see cref="BuildTarget"/>s the upscaler does not support. Set the <paramref name="graphicsDeviceTypes"/> property to rule out
        /// particular <see cref="GraphicsDeviceType"/>s on those targets.
        /// </summary>
        /// <param name="buildTargets">The <see cref="BuildTarget"/>s the upscaler does not support. Leave unset to apply to any target.</param>
        public UpscalerUnsupportedBuildTargetAttribute(params BuildTarget[] buildTargets)
        {
            this.buildTargets = buildTargets ?? Array.Empty<BuildTarget>();
        }
    }
}
#endif
#endif
