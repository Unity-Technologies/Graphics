using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    ///     Lightmapping utilities for HDRP
    /// </summary>
    public static class LightmappingHDRP
    {
        /// <summary>
        ///     Bake the <paramref name="probe" /> and updated its baked texture.
        ///
        ///     Note: The update of the probe is persistent only in editor mode.
        /// </summary>
        /// <param name="probe">The probe to bake.</param>
        /// <param name="path">The asset path to write the baked texture to.</param>
        /// <param name="options">The options to use for the bake.</param>
        /// <returns>
        ///     <c>null</c> on success or the error that occured.
        ///     The error can be:
        ///     * <see cref="ArgumentException" /> if the <paramref name="path" /> is invalid.
        ///     * <see cref="ArgumentNullException" /> if the <paramref name="probe" /> is <c>null</c>.
        ///     * <see cref="Exception" /> if the <paramref name="probe" /> is not supported. Only
        ///     <see cref="HDAdditionalReflectionData" /> probes are currently supported.
        /// </returns>
        public static Exception BakeProbe([NotNull] HDProbe probe, [NotNull] string path, BakeProbeOptions options)
        {
            // Check arguments
            if (probe == null || probe.Equals(null)) return new ArgumentNullException(nameof(probe));
            if (string.IsNullOrEmpty(path))
                return new ArgumentException($"{nameof(path)} must not be empty or null.");

            // We force RGBAHalf as we don't support 11-11-10 textures (only RT)
            var probeFormat = GraphicsFormat.R16G16B16A16_SFloat;
            switch (probe)
            {
                case HDAdditionalReflectionData _:
                {
                    // Get the texture size from the probe
                    var textureSize = options.textureSize.Evaluate(probe);

                    // Render and write
                    var cubeRT = HDRenderUtilities.CreateReflectionProbeRenderTarget(textureSize, probeFormat);
                    HDBakedReflectionSystem.RenderAndWriteToFile(probe, path, cubeRT, null);
                    cubeRT.Release();

                    // Import asset at target location
                    AssetDatabase.ImportAsset(path);
                    HDBakedReflectionSystem.ImportAssetAt(probe, path);

                    // Assign to the probe the baked texture
                    var bakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                    probe.SetTexture(ProbeSettings.Mode.Baked, bakedTexture);

                    // Mark probe as dirty
                    EditorUtility.SetDirty(probe);

                    return null;
                }
                case PlanarReflectionProbe _:
                    return new Exception("Planar reflection probe baking is not supported.");
                default: return new Exception($"Cannot handle probe type: {probe.GetType()}");
            }
        }

        /// <summary>
        ///     Options of <see cref="LightmappingHDRP.BakeProbe" /> function.
        /// </summary>
        public struct BakeProbeOptions
        {
            /// <summary>
            ///     Determines the size of the baked texture.
            /// </summary>
            public struct TextureSize
            {
                /// <summary>
                ///     Which methods to use to evaluate the value.
                /// </summary>
                public enum Mode
                {
                    /// <summary>
                    ///     Use the probe resolution as texture size.
                    /// </summary>
                    UseProbeResolution,

                    /// <summary>
                    ///     Use the value of <see cref="TextureSize.customValue" />.
                    /// </summary>
                    CustomValue
                }

                /// <summary>
                ///     Returns a <see cref="TextureSize" /> with default values.
                /// </summary>
                /// <returns>A <see cref="TextureSize" /> with default values.</returns>
                public static TextureSize NewDefault()
                {
                    return new TextureSize {mode = Mode.UseProbeResolution};
                }

                /// <summary>
                ///     The mode to use.
                /// </summary>
                public Mode mode;

                /// <summary>
                ///     The value used in the <see cref="Mode.CustomValue" /> mode.
                /// </summary>
                public int customValue;

                /// <summary>
                ///     Evaluate the texture size to use for baking.
                /// </summary>
                /// <param name="probe">The probe that will be baked.</param>
                /// <returns>The value of the texture size to use for the bake.</returns>
                /// <exception cref="ArgumentNullException">
                ///     When <paramref name="probe" /> is <c>null</c> and the mode
                ///     <see cref="Mode.UseProbeResolution" /> is used.
                /// </exception>
                /// <exception cref="ArgumentOutOfRangeException">When <see cref="mode" /> has an invalid value.</exception>
                public int Evaluate(HDProbe probe)
                {
                    switch (mode)
                    {
                        case Mode.CustomValue: return customValue;
                        case Mode.UseProbeResolution:
                        {
                            if (probe == null || probe.Equals(null)) throw new ArgumentNullException(nameof(probe));
                            return (int) probe.resolution;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(mode));
                    }
                }
            }

            /// <summary>
            ///     Return a <see cref="BakeProbeOptions" /> with default values.
            /// </summary>
            /// <returns>A <see cref="BakeProbeOptions" /> with default values.</returns>
            public static BakeProbeOptions NewDefault()
            {
                return new BakeProbeOptions
                {
                    textureSize = TextureSize.NewDefault()
                };
            }

            /// <summary>
            ///     The texture size to use for the bake.
            /// </summary>
            public TextureSize textureSize;
        }
    }
}
