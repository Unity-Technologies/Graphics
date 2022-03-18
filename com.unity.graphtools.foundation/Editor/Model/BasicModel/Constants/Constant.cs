using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
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
        public virtual T Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <inheritdoc />
        public object ObjectValue
        {
            get => Value;
            set => Value = FromObject(value);
        }

        /// <inheritdoc />
        public virtual object DefaultValue => default(T);

        /// <inheritdoc />
        public virtual Type Type => typeof(T);

        /// <inheritdoc />
        public virtual void Initialize(TypeHandle constantTypeHandle)
        {
            Debug.Assert(constantTypeHandle.Resolve().IsAssignableFrom(GetTypeHandle().Resolve()));
            ObjectValue = DefaultValue;
        }

        /// <inheritdoc />
        public virtual IConstant Clone()
        {
            var copy = (Constant<T>)Activator.CreateInstance(GetType());
            copy.ObjectValue = ObjectValue;
            return copy;
        }

        /// <inheritdoc />
        public virtual TypeHandle GetTypeHandle()
        {
            return Type.GenerateTypeHandle();
        }

        /// <summary>
        /// Converts an object to a value of the type {T}.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>The object cast to type {T}.</returns>
        protected virtual T FromObject(object value) => (T)value;
    }
}
