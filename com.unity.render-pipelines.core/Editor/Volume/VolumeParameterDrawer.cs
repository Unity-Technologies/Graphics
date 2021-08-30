using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// This attributes tells an <see cref="VolumeParameterDrawer"/> class which type of
    /// <see cref="VolumeParameter"/> it's an editor for.
    /// When you make a custom drawer for a parameter, you need add this attribute to the drawer
    /// class.
    /// </summary>
    /// <seealso cref="VolumeParameterDrawer"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VolumeParameterDrawerAttribute : Attribute
    {
        /// <summary>
        /// A type derived from <see cref="VolumeParameter{T}"/>.
        /// </summary>
        public readonly Type parameterType;

        /// <summary>
        /// Creates a new <see cref="VolumeParameterDrawerAttribute"/> instance.
        /// </summary>
        /// <param name="parameterType">A type derived from <see cref="VolumeParameter{T}"/>.</param>
        public VolumeParameterDrawerAttribute(Type parameterType)
        {
            this.parameterType = parameterType;
        }
    }

    /// <summary>
    /// A base class to implement to draw custom editors for custom <see cref="VolumeParameter"/>.
    /// You must use a <see cref="VolumeParameterDrawerAttribute"/> to let the editor know which
    /// parameter this drawer is for.
    /// </summary>
    /// <remarks>
    /// If you do not provide a custom editor for a <see cref="VolumeParameter"/>, Unity uses the buil-in property drawers to draw the
    /// property as-is.
    /// </remarks>
    /// <example>
    /// Here's an example about how <see cref="ClampedFloatParameter"/> is implemented:
    /// <code>
    /// [VolumeParameterDrawer(typeof(ClampedFloatParameter))]
    /// class ClampedFloatParameterDrawer : VolumeParameterDrawer
    /// {
    ///     public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
    ///     {
    ///         var value = parameter.value;
    ///
    ///         if (value.propertyType != SerializedPropertyType.Float)
    ///             return false;
    ///
    ///         var o = parameter.GetObjectRef&lt;ClampedFloatParameter&gt;();
    ///         EditorGUILayout.Slider(value, o.min, o.max, title);
    ///         value.floatValue = Mathf.Clamp(value.floatValue, o.min, o.max);
    ///         return true;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="VolumeParameterDrawerAttribute"/>
    public abstract class VolumeParameterDrawer
    {
        // Override this and return false if you want to customize the override checkbox position,
        // else it'll automatically draw it and put the property content in a horizontal scope.

        /// <summary>
        /// Override this and return <c>false</c> if you want to customize the position of the override
        /// checkbox. If you don't, Unity automatically draws the checkbox and puts the property content in a
        /// horizontal scope.
        /// </summary>
        /// <returns><c>false</c> if the override checkbox position is customized, <c>true</c>
        /// otherwise</returns>
        public virtual bool IsAutoProperty() => true;

        /// <summary>
        /// Draws the parameter in the editor. If the input parameter is invalid you should return
        /// <c>false</c> so that Unity displays the default editor for this parameter.
        /// </summary>
        /// <param name="parameter">The parameter to draw.</param>
        /// <param name="title">The label and tooltip of the parameter.</param>
        /// <returns><c>true</c> if the input parameter is valid, <c>false</c> otherwise in which
        /// case Unity will revert to the default editor for this parameter</returns>
        public abstract bool OnGUI(SerializedDataParameter parameter, GUIContent title);
    }
}
