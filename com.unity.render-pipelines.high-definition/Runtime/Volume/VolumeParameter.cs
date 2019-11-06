using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public class InjectionPointParameter : VolumeParameter<CustomPostProcessInjectionPoint>
    {
        /// <summary>
        /// Creates a new <seealso cref="InjectionPointParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to be stored in the parameter</param>
        /// <param name="overrideState">The initial override state for the parameter</param>
        public InjectionPointParameter(CustomPostProcessInjectionPoint value, bool overrideState = false)
            : base(value, overrideState) {}

    }
}
