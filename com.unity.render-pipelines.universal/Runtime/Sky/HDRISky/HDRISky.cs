using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.Universal
{
    [VolumeComponentMenu("Sky/HDRI Sky")]
    [SkyUniqueID((int)SkyType.HDRI)]
    public class HDRISky : SkySettings
    {
        /// <summary>Cubemap used to render the HDRI sky.</summary>
        [Tooltip("Specify the cubemap HDRP uses to render the sky.")]
        public CubemapParameter hdriSky = new CubemapParameter(null);

        // TODO Other params

        /// <summary>Enable Backplate to have it visible.</summary>
        [Tooltip("Enable or disable the backplate.")]
        public BoolParameter enableBackplate = new BoolParameter(false);
        /// <summary>Backplate Type {Disc, Rectangle, Ellipse, Infinite (Plane)}.</summary>
        [Tooltip("Backplate type.")]
        public BackplateTypeParameter backplateType = new BackplateTypeParameter(BackplateType.Disc);
        /// <summary>Define the ground level of the Backplate.</summary>
        [Tooltip("Define the ground level of the Backplate.")]
        public FloatParameter groundLevel = new FloatParameter(0.0f);
        /// <summary>Extent of the Backplate (if circle only the X value is considered).</summary>
        [Tooltip("Extent of the Backplate (if circle only the X value is considered).")]
        public Vector2Parameter scale = new Vector2Parameter(Vector2.one * 32.0f);
        /// <summary>Backplate's projection distance to varying the cubemap projection on the plate.</summary>
        [Tooltip("Backplate's projection distance to varying the cubemap projection on the plate.")]
        public MinFloatParameter projectionDistance = new MinFloatParameter(16.0f, 1e-7f);
        /// <summary>Backplate rotation parameter for the geometry.</summary>
        [Tooltip("Backplate rotation parameter for the geometry.")]
        public ClampedFloatParameter plateRotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        /// <summary>Backplate rotation parameter for the projected texture.</summary>
        [Tooltip("Backplate rotation parameter for the projected texture.")]
        public ClampedFloatParameter plateTexRotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        /// <summary>Backplate projection offset on the plane.</summary>
        [Tooltip("Backplate projection offset on the plane.")]
        public Vector2Parameter plateTexOffset = new Vector2Parameter(Vector2.zero);
        /// <summary>Backplate blend parameter to blend the edge of the backplate with the background.</summary>
        [Tooltip("Backplate blend parameter to blend the edge of the backplate with the background.")]
        public ClampedFloatParameter blendAmount = new ClampedFloatParameter(0.0f, 0.0f, 100.0f);

        // TODO Shadow parameters


        /// <summary>
        /// Returns the hash code of the HDRI sky parameters.
        /// </summary>
        /// <returns>The hash code of the HDRI sky parameters.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            unchecked
            {
                hash = hdriSky.value != null ? hash * 23 + hdriSky.GetHashCode() : hash;

                hash = hash * 23 + enableBackplate.GetHashCode();
                hash = hash * 23 + backplateType.GetHashCode();
                hash = hash * 23 + groundLevel.GetHashCode();
                hash = hash * 23 + scale.GetHashCode();
                hash = hash * 23 + projectionDistance.GetHashCode();
                hash = hash * 23 + plateRotation.GetHashCode();
                hash = hash * 23 + plateTexRotation.GetHashCode();
                hash = hash * 23 + plateTexOffset.GetHashCode();
                hash = hash * 23 + blendAmount.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Returns HDRISkyRenderer type.
        /// </summary>
        /// <returns>HDRISkyRenderer type.</returns>
        public override Type GetSkyRendererType() { return typeof(HDRISkyRenderer); }
    }

    /// <summary>
    /// Backplate Type for HDRISKy.
    /// </summary>
    public enum BackplateType
    {
        /// <summary>Shape of backplate is a Disc.</summary>
        Disc,
        /// <summary>Shape of backplate is a Rectangle.</summary>
        Rectangle,
        /// <summary>Shape of backplate is a Ellispe.</summary>
        Ellipse,
        /// <summary>Shape of backplate is a Infinite Plane.</summary>
        Infinite
    }

    /// <summary>
    /// Backplate Type volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class BackplateTypeParameter : VolumeParameter<BackplateType>
    {
        /// <summary>
        /// Backplate Type volume parameter constructor.
        /// </summary>
        /// <param name="value">Backplate Type parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public BackplateTypeParameter(BackplateType value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
