using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// This attribute is used to associate a unique ID to a sky class.
    /// This is needed to be able to automatically register sky classes and avoid collisions and refactoring class names causing data compatibility issues.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SkyUniqueID : Attribute
    {
        internal readonly int uniqueID;

        /// <summary>
        /// Attribute SkyUniqueID constructor.
        /// </summary>
        /// <param name="uniqueID">Sky unique ID. Needs to be different from all other registered unique IDs.</param>
        public SkyUniqueID(int uniqueID)
        {
            this.uniqueID = uniqueID;
        }
    }

    /// <summary>
    /// Environment Update volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class EnvUpdateParameter : VolumeParameter<EnvironmentUpdateMode>
    {
        /// <summary>
        /// Environment Update parameter constructor.
        /// </summary>
        /// <param name="value">Environment Update Mode parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public EnvUpdateParameter(EnvironmentUpdateMode value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    /// <summary>
    /// Sky Intensity Mode.
    /// </summary>
    public enum SkyIntensityMode
    {
        /// <summary>Intensity is expressed as an exposure.</summary>
        Exposure,
        /// <summary>Intensity is expressed in lux.</summary>
        Lux,
        /// <summary>Intensity is expressed as a multiplier.</summary>
        Multiplier,
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

    /// <summary>
    /// Sky Intensity volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SkyIntensityParameter : VolumeParameter<SkyIntensityMode>
    {
        /// <summary>
        /// Sky Intensity volume parameter constructor.
        /// </summary>
        /// <param name="value">Sky Intensity parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public SkyIntensityParameter(SkyIntensityMode value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    /// <summary>
    /// Base class for custom Sky Settings.
    /// </summary>
    public abstract class SkySettings : VolumeComponent
    {
        /// <summary>Rotation of the sky.</summary>
        [Tooltip("Sets the rotation of the sky.")]
        public ClampedFloatParameter    rotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);
        /// <summary>Intensity mode of the sky.</summary>
        [Tooltip("Specifies the intensity mode HDRP uses for the sky.")]
        public SkyIntensityParameter    skyIntensityMode = new SkyIntensityParameter(SkyIntensityMode.Exposure);
        /// <summary>Exposure of the sky.</summary>
        [Tooltip("Sets the exposure of the sky in EV.")]
        public FloatParameter           exposure = new FloatParameter(0.0f);
        /// <summary>Intensity Multipler of the sky.</summary>
        [Tooltip("Sets the intensity multiplier for the sky.")]
        public MinFloatParameter        multiplier = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Informative helper that displays the relative intensity (in Lux) for the current HDR texture set in HDRI Sky.</summary>
        [Tooltip("Informative helper that displays the relative intensity (in Lux) for the current HDR texture set in HDRI Sky.")]
        public MinFloatParameter        upperHemisphereLuxValue = new MinFloatParameter(1.0f, 0.0f);
        /// <summary>Informative helper that displays Show the color of Shadow.</summary>
        [Tooltip("Informative helper that displays Show the color of Shadow.")]
        public Vector3Parameter         upperHemisphereLuxColor = new Vector3Parameter(new Vector3(0, 0, 0));
        /// <summary>Absolute intensity (in lux) of the sky.</summary>
        [Tooltip("Sets the absolute intensity (in Lux) of the current HDR texture set in HDRI Sky. Functions as a Lux intensity multiplier for the sky.")]
        public FloatParameter           desiredLuxValue = new FloatParameter(20000);
        /// <summary>Update mode of the sky.</summary>
        [Tooltip("Specifies when HDRP updates the environment lighting. When set to OnDemand, use HDRenderPipeline.RequestSkyEnvironmentUpdate() to request an update.")]
        public EnvUpdateParameter       updateMode = new EnvUpdateParameter(EnvironmentUpdateMode.OnChanged);
        /// <summary>In case of real-time update mode, time between updates. 0 means every frame.</summary>
        [Tooltip("Sets the period, in seconds, at which HDRP updates the environment ligting (0 means HDRP updates it every frame).")]
        public MinFloatParameter        updatePeriod = new MinFloatParameter(0.0f, 0.0f);
        /// <summary>True if the sun disk should be included in the baking information (where available).</summary>
        [Tooltip("When enabled, HDRP uses the Sun Disk in baked lighting.")]
        public BoolParameter            includeSunInBaking = new BoolParameter(false);


        static Dictionary<Type, int>  skyUniqueIDs = new Dictionary<Type, int>();

        /// <summary>
        /// Returns the hash code of the sky parameters.
        /// </summary>
        /// <returns>The hash code of the sky parameters.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
#if UNITY_2019_3 // In 2019.3, when we call GetHashCode on a VolumeParameter it generate garbage (due to the boxing of the generic parameter)
                // UpdateMode and period should not be part of the hash as they do not influence rendering itself.
                int hash = 13;
                hash = hash * 23 + rotation.value.GetHashCode();
                hash = hash * 23 + exposure.value.GetHashCode();
                hash = hash * 23 + multiplier.value.GetHashCode();
                hash = hash * 23 + desiredLuxValue.value.GetHashCode();
                hash = hash * 23 + skyIntensityMode.value.GetHashCode();
                hash = hash * 23 + includeSunInBaking.value.GetHashCode();

                hash = hash * 23 + rotation.overrideState.GetHashCode();
                hash = hash * 23 + exposure.overrideState.GetHashCode();
                hash = hash * 23 + multiplier.overrideState.GetHashCode();
                hash = hash * 23 + desiredLuxValue.overrideState.GetHashCode();
                hash = hash * 23 + skyIntensityMode.overrideState.GetHashCode();
                hash = hash * 23 + includeSunInBaking.overrideState.GetHashCode();
#else
                // UpdateMode and period should not be part of the hash as they do not influence rendering itself.
                int hash = 13;
                hash = hash * 23 + rotation.GetHashCode();
                hash = hash * 23 + exposure.GetHashCode();
                hash = hash * 23 + multiplier.GetHashCode();
                hash = hash * 23 + desiredLuxValue.GetHashCode();
                hash = hash * 23 + skyIntensityMode.GetHashCode();
                hash = hash * 23 + includeSunInBaking.GetHashCode();
#endif

                return hash;
            }
        }

        internal static int GetUniqueID<T>()
        {
            return GetUniqueID(typeof(T));
        }

        internal static int GetUniqueID(Type type)
        {
            int uniqueID;

            if (!skyUniqueIDs.TryGetValue(type, out uniqueID))
            {
                var uniqueIDs = type.GetCustomAttributes(typeof(SkyUniqueID), false);
                uniqueID = (uniqueIDs.Length == 0) ? -1 : ((SkyUniqueID)uniqueIDs[0]).uniqueID;
                skyUniqueIDs[type] = uniqueID;
            }

            return uniqueID;
        }

        /// <summary>
        /// Returns the class type of the SkyRenderer associated with this Sky Settings.
        /// </summary>
        /// <returns>The class type of the SkyRenderer associated with this Sky Settings.</returns>
        public abstract Type GetSkyRendererType();
    }
}
