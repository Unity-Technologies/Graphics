using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Use this attribute to show a warning next to a VolumeComponent's UI if the specified
    /// ScriptableRendererFeatures are not added to the active URP Asset's default renderer
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VolumeRequiresRendererFeatures : Attribute
    {
        internal HashSet<Type> TargetFeatureTypes;

        /// <summary>
        /// Creates a new <see cref="VolumeRequiresRendererFeatures"/> attribute instance.
        /// </summary>
        /// <param name="featureTypes">The list of required ScriptableRendererFeature types. If any of these types are missing, the VolumeComponent UI shows a warning.</param>
        public VolumeRequiresRendererFeatures(params Type[] featureTypes)
        {
            TargetFeatureTypes = (featureTypes != null) ? new HashSet<Type>(featureTypes) : new HashSet<Type>();
            TargetFeatureTypes.Remove(null);
        }
    }
}
