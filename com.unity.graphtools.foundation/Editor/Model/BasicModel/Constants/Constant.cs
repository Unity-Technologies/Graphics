using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Base implementation for constants.
    /// </summary>
    /// <typeparam name="T">The type of the value of the constant.</typeparam>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class Constant<T> : IConstant
    {
        [SerializeField]
        protected T m_Value;

        /// <summary>
        /// The constant value.
        /// </summary>
        public T Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <inheritdoc />
        public object ObjectValue
        {
            get => m_Value;
            set => m_Value = FromObject(value);
        }

        /// <inheritdoc />
        public virtual object DefaultValue => default(T);

        /// <inheritdoc />
        public virtual Type Type => typeof(T);

        /// <summary>
        /// Converts an object to a value of the type {T}.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>The object cast to type {T}.</returns>
        protected virtual T FromObject(object value) => (T)value;
    }
}
